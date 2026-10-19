using System.Windows;
using Memento.Shell;

namespace Memento;

/// <summary>Holds the application resources only; startup lives in <see cref="Shell.AppStartup"/>.</summary>
public partial class App : Application
{
    public App() => InitializeComponent();
}
