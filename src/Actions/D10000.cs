namespace Loupedeck.DicePlugin.Actions
{
    using System;

    internal class D10000 : PluginDynamicCommand
    {
        private Random random = new Random();
        private int hand;
        private const int value = 10000;

        private int lineHeight = 32;
        private int fontSize = 32;

        public D10000()
            : base(displayName: $"D{value}", description: $"Rolls a {value}", groupName: "Dice")
        {
            base.IsWidget = true;
        }

        protected override void RunCommand(String actionParameter)
        {
            rollInProgress();
            this.hand = random.Next(value) + 1;
            rollDone();
            PluginLog.Info($"Throw value is {this.hand}");
        }

        private void rollInProgress()
        {
            this.lineHeight = 16;
            this.fontSize = 16;
            this.ActionImageChanged();
            Thread.Sleep(250);
        }
        private void rollDone()
        {
            this.lineHeight = 32;
            this.fontSize = 32;
            this.ActionImageChanged();
        }


        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            using (var bitmapBuilder = new BitmapBuilder(imageSize))
            {

                bitmapBuilder.DrawText($"D{value}{Environment.NewLine}{this.hand}", fontSize: this.fontSize, lineHeight: this.lineHeight);

                return bitmapBuilder.ToImage();
            }
        }
    }
}