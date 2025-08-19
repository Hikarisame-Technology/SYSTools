using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Media.Animation;
using System;
using System.Collections.Generic;
using SYSTools.ToolPages;
using Page = System.Windows.Controls.Page;


namespace SYSTools.Pages
{
    /// <summary>
    /// Toolkit.xaml 的交互逻辑
    /// </summary>
    public partial class Toolkit : Page
    {
        private readonly Dictionary<Type, Page> _pages = new()
        {
            { typeof(DetectionTools), new DetectionTools() },
            { typeof(TestTools), new TestTools() },
            { typeof(DiskTools), new DiskTools() },
            { typeof(PeripheralsTools), new PeripheralsTools() },
            { typeof(RepairingTools), new RepairingTools() },
            { typeof(UncategorizedTools), new UncategorizedTools() }
        };

        private Type _currentPageType = typeof(DetectionTools);
        private bool _isInitialized = false;

        public Toolkit()
        {
            InitializeComponent();
        }

        private void NavigationTriggered(
            NavigationView sender,
            NavigationViewItemInvokedEventArgs args
        )
        {
            if (args.InvokedItemContainer?.Tag is string tag)
            {
                Type targetType = Type.GetType(tag);
                if (targetType != null)
                {
                    // 更新当前页面类型跟踪
                    _currentPageType = targetType;
                    // 统一切换动画
                    NavigateTo(targetType, new DrillInNavigationTransitionInfo());
                }
            }
        }

        private void NavigateTo(Type navPageType, NavigationTransitionInfo transitionInfo)
        {
            // 导航到目标页
            var preNavPageType = CurrentPage.Content?.GetType();
            if (navPageType == preNavPageType) return;
            transitionInfo ??= new DrillInNavigationTransitionInfo();
            if (_pages.TryGetValue(navPageType, out var page))
            {
                CurrentPage.Navigate(page, transitionInfo);
            }
            else
            {
                // 处理未注册页面的逻辑
                throw new InvalidOperationException($"Page type {navPageType.Name} is not registered.");
            }
        }

        private void SetSelectedNavigationItem(Type pageType)
        {
            // 根据页面类型设置NavigationView的选中状态
            foreach (var item in ToolsNavigation.MenuItems)
            {
                if (item is NavigationViewItem navItem && navItem.Tag is string tag)
                {
                    Type itemType = Type.GetType(tag);
                    if (itemType == pageType)
                    {
                        ToolsNavigation.SelectedItem = navItem;
                        break;
                    }
                }
            }
        }


        private void Window_Loading(object sender, System.Windows.RoutedEventArgs e)
        {
            // 只在第一次初始化时导航到默认页面
            if (!_isInitialized)
            {
                NavigateTo(_currentPageType, new DrillInNavigationTransitionInfo());
                SetSelectedNavigationItem(_currentPageType);
                _isInitialized = true;
            }
            else
            {
                // 如果已经初始化过，恢复到之前的页面状态
                NavigateTo(_currentPageType, new DrillInNavigationTransitionInfo());
                SetSelectedNavigationItem(_currentPageType);
            }
        }
    }
}
