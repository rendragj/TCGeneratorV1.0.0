using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using TCGeneratorV1._0._0.View;

namespace TCGeneratorV1._0._0.ViewModel
{
    public class TabItemsVM
    {
        public string Header { get; set; }
        public UserControl Content { get; set; }
        public ImageSource IconSource { get; set; }
    }
}
