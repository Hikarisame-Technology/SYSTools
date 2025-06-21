using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SYSTools.ViewModels;

namespace SYSTools.Pages
{
    public partial class CustomTool : Page
    {
        public CustomTool()
        {
            InitializeComponent();
            DataContext = new CustomToolViewModel();
        }
        private void ToolItemContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu menu)
            {
                menu.DataContext = this.DataContext;
            }
        }
    }
}
