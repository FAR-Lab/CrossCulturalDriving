using System.Collections.Generic;
using UltimateReplay.Storage;

namespace UltimateReplay.Core.Services
{
    internal class ReplayRecordServiceInstance : ReplayServiceInstance
    {
        // Private
        private static Stack<ReplayRecordServiceInstance> waitingServices = new Stack<ReplayRecordServiceInstance>();

        private ReplayRecordOptions recordOptions = null;
        private ReplayTimer recordTimer = new ReplayTimer();
        private ReplayTimer updateTimer = new ReplayTimer();
        private int recordSnapshotSequence = ReplaySnapshot.startSequenceID;

        // Properties
        public override UltimateReplay.UpdateMethod UpdateMethod
        {
            get { return recordOptions.RecordUpdateMethod; }
        }

        // Methods
        public void Initialize(ReplayHandle handle, ReplayServiceState state, ReplayScene scene, ReplayStorageTarget target, ReplayRecordOptions recordOptions)
        {
            // Initilaize base class
            base.Initialize(handle, state, scene, target);

            this.recordOptions = recordOptions;
            this.recordTimer.Reset();
            this.updateTimer.Reset();
            this.recordSnapshotSequence = ReplaySnapshot.startSequenceID;
        }

        public void ReplayRecordInitialFrame()
        {
            if (recordSnapshotSequence == ReplaySnapshot.startSequenceID)
            {
                // Send capture event
                ReplayBehaviour.InvokeReplayCaptureEvent(scene.ActiveReplayBehaviours);

                // Create the initial snapshot
                ReplaySnapshot initialSnapshot = scene.CaptureSnapshot(0f, ReplaySnapshot.startSequenceID, target.InitialStateBuffer);
                
                // Record to target
                target.StoreSnapshot(initialSnapshot);

                // Ensure validity after storing because compression could take place
                initialSnapshot.VerifySnapshot(true);
                recordSnapshotSequence++;
            }
        }

        public override void ReplayUpdate(float deltaTime)
        {
            // Update the timers
            recordTimer.Tick(deltaTime);
            updateTimer.Tick(deltaTime);

            // Calcualate record frame interval
            float interval = (1.0f / recordOptions.RecordFPS);

            // Check for elapsed time
            if (updateTimer.HasElapsed(interval) == true)
            {
                // Reset timer
                updateTimer.Reset();

                // Send capture event
                ReplayBehaviour.InvokeReplayCaptureEvent(scene.ActiveReplayBehaviours);

                // Get the scene state
                ReplaySnapshot recordSnapshot = scene.CaptureSnapshot(recordTimer.ElapsedSeconds, recordSnapshotSequence, target.InitialStateBuffer);
                
                // Record the snapshot
                target.StoreSnapshot(recordSnapshot);

                // Ensure validity after storing because compression could take place
                recordSnapshot.VerifySnapshot(true);

                // Update sequence id
                recordSnapshotSequence++;
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            this.recordOptions = null;
            this.recordTimer = new ReplayTimer();
            this.updateTimer = new ReplayTimer();
            this.recordSnapshotSequence = ReplaySnapshot.startSequenceID;

            waitingServices.Push(this);
        }

        public static ReplayRecordServiceInstance GetPooledServiceInstance()
        {
            // Reuse pooled instance
            if (waitingServices.Count > 0)
                return waitingServices.Pop();

            // Create new instance
            return new ReplayRecordServiceInstance();
        }
    }
}
