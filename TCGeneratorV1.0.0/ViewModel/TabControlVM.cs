using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using TCGeneratorV1._0._0.View;

namespace TCGeneratorV1._0._0.ViewModel
{
    public class TabControlVM
    {
        public ObservableCollection<TabItemsVM> TabItems { get; }

        public TabControlVM()
        {
            TabItems = new ObservableCollection<TabItemsVM>
            {
                new TabItemsVM
                {
                    Header = "Welcome",
                    IconSource = LoadImageFromResource("vailogo.png"),
                    Content = new Home()
                },
                new TabItemsVM
                {
                    Header = "Pokemon",
                    IconSource = LoadImageFromResource("pkmanlogo.png"),
                    Content = new Pokemon()
                },
                new TabItemsVM
                {
                    Header = "Magic The Gathering",
                    IconSource = LoadImageFromResource("mtglogo.png"),
                    Content = new MTG()
                }
            };
        }

        private BitmapImage LoadImageFromResource(string imageName)
        {
            var uri = new Uri($"pack://application:,,,/Resources/{imageName}", UriKind.RelativeOrAbsolute);
            BitmapImage bitmap = new BitmapImage(uri);
            return bitmap;
        }

        private double baseTabHeaderWidth = 100;
        public double TabHeaderWidth => baseTabHeaderWidth * TabItems.Count;
    }
}
