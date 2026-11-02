using System.ComponentModel.Composition;
using System.Windows;

namespace WindowsGpsLocation {

    [Export(typeof(ResourceDictionary))]
    public partial class DataTemplates : ResourceDictionary {

        public DataTemplates() {
            InitializeComponent();
        }
    }
}
