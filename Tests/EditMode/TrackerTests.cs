using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Objectives.Tests
{
    public class TrackerTests
    {
        sealed class TestObjective : Objective
        {
            public int signals;
            public int resets;
            public void Finish() => Complete();
            public void Work(float p) => SetState(ObjectiveState.Active, p);
            protected override void OnSignal(ulong senderClientId, int value) => signals += value;
            protected override void OnReset() => resets++;
        }

        readonly List<GameObject> created = new();

        GameObject Make(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null)
                go.transform.SetParent(parent);
            created.Add(go);
            return go;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in created)
                if (go != null)
                    Object.DestroyImmediate(go);
            created.Clear();
        }

        (ObjectiveTracker tracker, Transform root, TestObjective[] objectives) Level(int count)
        {
            var tracker = Make("Tracker").AddComponent<ObjectiveTracker>();
            var root = Make("Level").transform;
            var list = new TestObjective[count];
            for (int i = 0; i < count; i++)
                list[i] = Make($"Objective {i}", Make($"Piece {i}", root).transform).AddComponent<TestObjective>();
            return (tracker, root, list);
        }

        [Test]
        public void CollectsInHierarchyOrder()
        {
            var (tracker, root, objectives) = Level(3);
            tracker.Collect(root);
            Assert.AreEqual(3, tracker.Objectives.Count);
            for (int i = 0; i < 3; i++)
            {
                Assert.AreSame(objectives[i], tracker.Objectives[i]);
                Assert.AreEqual(i, objectives[i].Index);
                Assert.AreSame(tracker, objectives[i].Tracker);
            }
        }

        [Test]
        public void AllCompleteOnceEveryRequiredObjectiveIs()
        {
            var (tracker, root, objectives) = Level(3);
            objectives[2].Required = false;
            int completed = 0;
            tracker.Completed += _ => completed++;
            tracker.Collect(root);

            Assert.AreEqual(2, tracker.RequiredCount);
            Assert.IsFalse(tracker.AllComplete);
            objectives[0].Finish();
            Assert.AreEqual(1, tracker.CompletedCount);
            Assert.IsFalse(tracker.AllComplete);
            objectives[1].Finish();
            Assert.IsTrue(tracker.AllComplete);
            Assert.AreEqual(1, completed);
            objectives[1].Finish();
            Assert.AreEqual(1, completed, "Completed is raised once.");
        }

        [Test]
        public void ProgressAndStateAreReported()
        {
            var (tracker, root, objectives) = Level(1);
            tracker.Collect(root);
            int changes = 0;
            objectives[0].Changed += _ => changes++;
            objectives[0].Work(0.5f);
            Assert.AreEqual(ObjectiveState.Active, objectives[0].State);
            Assert.AreEqual(0.5f, objectives[0].Progress, 0.01f);
            objectives[0].Work(0.5f);
            Assert.AreEqual(1, changes, "Same status twice is one change.");
        }

        [Test]
        public void RecollectStartsOver()
        {
            var (tracker, root, objectives) = Level(2);
            tracker.Collect(root);
            objectives[0].Finish();
            objectives[1].Finish();
            Assert.IsTrue(tracker.AllComplete);
            tracker.Collect(root);
            Assert.IsFalse(tracker.AllComplete);
            Assert.AreEqual(ObjectiveState.Incomplete, objectives[0].State);
            Assert.AreEqual(2, objectives[0].resets);
        }

        [Test]
        public void ResetAndForceComplete()
        {
            var (tracker, root, objectives) = Level(1);
            tracker.Collect(root);
            objectives[0].ForceComplete();
            Assert.IsTrue(tracker.AllComplete);
            objectives[0].ResetObjective();
            Assert.IsFalse(tracker.AllComplete);
            tracker.CompleteAll();
            Assert.IsTrue(tracker.AllComplete);
        }

        [Test]
        public void SignalsReachTheAuthorityOffline()
        {
            var (tracker, root, objectives) = Level(1);
            tracker.Collect(root);
            objectives[0].Signal(3);
            Assert.AreEqual(3, objectives[0].signals);
        }

        [Test]
        public void NoObjectivesMeansOpen()
        {
            var tracker = Make("Tracker").AddComponent<ObjectiveTracker>();
            tracker.Collect(Make("Empty").transform);
            Assert.IsTrue(tracker.AllComplete);
        }

        [Test]
        public void StatusQuantisesProgress()
        {
            Assert.AreEqual(255, new ObjectiveStatus(ObjectiveState.Complete, 2f).progress);
            Assert.AreEqual(0, new ObjectiveStatus(ObjectiveState.Incomplete, -1f).progress);
            Assert.AreEqual(new ObjectiveStatus(ObjectiveState.Active, 0.5f), new ObjectiveStatus(ObjectiveState.Active, 0.5f));
        }

        [Test]
        public void AreaStandsOnItsTransform()
        {
            var owner = Make("Area").transform;
            owner.position = new Vector3(10f, 0f, 0f);
            var area = new ObjectiveArea(Vector3.zero, new Vector3(2f, 2f, 2f));
            Assert.IsTrue(area.Contains(owner, new Vector3(10.5f, 1f, 0f)));
            Assert.IsFalse(area.Contains(owner, new Vector3(12f, 1f, 0f)));
            Assert.IsFalse(area.Contains(owner, new Vector3(10f, 3f, 0f)));
            Assert.IsFalse(area.Contains(owner, new Vector3(10f, -1f, 0f)));
        }
    }
}
