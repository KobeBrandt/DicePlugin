namespace Loupedeck.DicePlugin.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class CustomDice : PluginDynamicCommand
    {
        private Int32 diceSides = CustomDiceVar.getDiceSides();
        private Random random = new Random();
        private Int32 currentRoll = 0;
        private Int32 font = 32;

        public CustomDice()
    : base(displayName: "Custom dice", description: "A dice with a value you set!", groupName: "Custom")
        {
            base.IsWidget = true;
        }

        protected override void RunCommand(String actionParameter)
        {
            this.diceSides = CustomDiceVar.getDiceSides();
            this.rollInProgress();
            this.currentRoll = this.random.Next(this.diceSides) + 1;
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

                bitmapBuilder.DrawText($"D{this.diceSides}{Environment.NewLine}{this.currentRoll}", fontSize: this.font, lineHeight: this.font);

                return bitmapBuilder.ToImage();
            }
        }
    }
}