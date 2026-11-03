namespace Loupedeck.DicePlugin.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal abstract class Dxxx : PluginDynamicCommand
    {
        private Int32 dieSides;
        private Random random = new Random();
        private Int32 currentRoll = 0;
        private const Int32 RollFrames = 5;
        private const Int32 RollFrameMilliseconds = 50;
        private Boolean isRolling = false;
        private Single rollingRotation = 0;

        public Dxxx(Int32 diceSides, String groupName = "")
    : base(displayName: $"D{diceSides}", description: $"Rolls a {diceSides}", groupName: groupName)
        {
            this.dieSides = diceSides;
            base.IsWidget = true;
        }

        protected override void RunCommand(String actionParameter)
        {
            this.rollInProgress();
            this.currentRoll = this.random.Next(this.dieSides) + 1;
            this.rollDone();
            PluginLog.Info($"Throw diceSides is {this.currentRoll}");
        }

        private void rollInProgress()
        {
            this.isRolling = true;
            for (var frame = 0; frame < RollFrames; frame++)
            {
                this.rollingRotation = (frame + 1) * MathF.Tau / (RollFrames + 1);
                this.ActionImageChanged();
                Thread.Sleep(RollFrameMilliseconds);
            }
        }
        private void rollDone()
        {
            this.isRolling = false;
            this.ActionImageChanged();
        }


        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            using (var bitmapBuilder = new BitmapBuilder(imageSize.GetButtonWidth(), imageSize.GetButtonHeight()))
            {
                var state = this.isRolling ? FaceState.Rolling : this.currentRoll != 0 ? FaceState.Result : FaceState.Idle;
                DieFace.Draw(bitmapBuilder, this.dieSides, state, this.currentRoll, this.rollingRotation);
                return bitmapBuilder.ToImage();
            }
        }
    }
}
