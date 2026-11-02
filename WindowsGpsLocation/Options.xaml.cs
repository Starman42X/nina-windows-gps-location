using System.ComponentModel.Composition;
using System.Windows;

namespace WindowsGpsLocation {

    [Export(typeof(ResourceDictionary))]
    public partial class Options : ResourceDictionary {

        public Options() {
            InitializeComponent();
        }
    }
}
