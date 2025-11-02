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
        private Int32 font = 32;

        public Dxxx(Int32 diceSides, String groupName = "Dice")
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
            this.font = 16;
            this.ActionImageChanged();
            Thread.Sleep(250);
        }
        private void rollDone()
        {
            this.font = 32;
            this.ActionImageChanged();
        }


        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            using (var bitmapBuilder = new BitmapBuilder(imageSize))
            {
                if (this.currentRoll != 0)
                {
                    bitmapBuilder.DrawText($"D{this.dieSides}{Environment.NewLine}{this.currentRoll}", fontSize: this.font, lineHeight: this.font);
                }
                else
                {
                    bitmapBuilder.DrawText($"D{this.dieSides}{Environment.NewLine}", fontSize: this.font, lineHeight: this.font);
                }


                    return bitmapBuilder.ToImage();
            }
        }
    }
}
