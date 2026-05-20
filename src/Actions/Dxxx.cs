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
        private Int32 fontSizeChanger = 3;

        public Dxxx(Int32 diceSides, String groupName = "pre made")
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
            this.fontSizeChanger = 5;
            this.ActionImageChanged();
            Thread.Sleep(250);
        }
        private void rollDone()
        {
            this.fontSizeChanger = 3;
            this.ActionImageChanged();
        }


        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            int fontSize = imageSize.GetSize() / this.fontSizeChanger;
            using (var bitmapBuilder = new BitmapBuilder(imageSize))
            {
                if (this.currentRoll != 0)
                {
                    bitmapBuilder.DrawText($"D{this.dieSides}{Environment.NewLine}{this.currentRoll}", fontSize: fontSize, lineHeight: fontSize);
                }
                else
                {
                    bitmapBuilder.DrawText($"D{this.dieSides}{Environment.NewLine}", fontSize: fontSize, lineHeight: fontSize);
                }


                    return bitmapBuilder.ToImage();
            }
        }
    }
}
