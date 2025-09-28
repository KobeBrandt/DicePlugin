namespace Loupedeck.DicePlugin.Actions
{
    using System;

    internal class D6 : PluginDynamicCommand
    {
        private Random random = new Random();
        private int hand;
        private const int value = 6;


        private int lineHeight = 32;
        private int fontSize = 32;

        // Initializes the command class.
        public D6()
            : base(displayName: $"D{value}", description: $"Rolls a {value}", groupName: "Dice")
        {
            base.IsWidget = true;
        }

        // This method is called when the user executes the command.
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
            Thread.Sleep(500);
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