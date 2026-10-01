using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RibbonSpace;

/// <summary>
/// Minimal <see cref="INotifyPropertyChanged"/> base used by every RibbonSpace model.
/// Compatible with any MVVM framework (CommunityToolkit.Mvvm, ReactiveUI, Prism, ...).
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Raises <see cref="PropertyChanged"/>.</summary>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>Sets <paramref name="field"/> and raises <see cref="PropertyChanged"/> when the value changed.</summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
