namespace Loupedeck.DicePlugin.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal abstract class Dxxx : PluginDynamicCommand
    {
        private int diceSides;
        private Random random = new Random();
        private int currentRoll = 0;
        private int font = 32;

        public Dxxx(int diceSides)
    : base(displayName: $"D{diceSides}", description: $"Rolls a {diceSides}", groupName: "Dice")
        {
            this.diceSides = diceSides;
            base.IsWidget = true;
        }

        protected override void RunCommand(String actionParameter)
        {
            rollInProgress();
            this.currentRoll = random.Next(diceSides) + 1;
            rollDone();
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

                bitmapBuilder.DrawText($"D{diceSides}{Environment.NewLine}{this.currentRoll}", fontSize: this.font, lineHeight: this.font);

                return bitmapBuilder.ToImage();
            }
        }
    }
}
