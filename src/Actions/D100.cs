namespace Loupedeck.DicePlugin.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class D100 : PluginDynamicCommand
    {
        private Random random = new Random();
        private int hand;
        private const int value = 100;

        public D100()
            : base(displayName: $"D{value}", description: $"Rolls a {value}", groupName: "Dice")
        {
            base.IsWidget = true;
        }

        protected override void RunCommand(String actionParameter)
        {
            this.hand = random.Next(value) + 1;
            this.ActionImageChanged();
            PluginLog.Info($"Throw value is {this.hand}");
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            using (var bitmapBuilder = new BitmapBuilder(imageSize))
            {
                var lineHeight = 32;
                var fontSize = 32;
                bitmapBuilder.DrawText($"D{value}{Environment.NewLine}{this.hand}", fontSize: fontSize, lineHeight: lineHeight);




                return bitmapBuilder.ToImage();
            }
        }
    }
}