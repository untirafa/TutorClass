using Microsoft.Maui.Controls.Shapes;

namespace TutorClass.Mobile.PrototypeUI.Styles;

// Page-local only. No Application resources, handlers or business state are modified.
public static class PrototypeTheme
{
    public static ResourceDictionary Create()
    {
        var resources = new ResourceDictionary();
        resources.Add(new Style(typeof(Label))
        {
            Setters =
            {
                new Setter { Property = Label.TextColorProperty, Value = Color.FromArgb("#101A2E") },
                new Setter { Property = Label.FontSizeProperty, Value = 16d },
                new Setter { Property = Label.LineBreakModeProperty, Value = LineBreakMode.WordWrap },
                new Setter { Property = Label.LineHeightProperty, Value = 1.15d }
            }
        });
        resources.Add(new Style(typeof(Button))
        {
            Setters =
            {
                new Setter { Property = VisualElement.MinimumHeightRequestProperty, Value = 52d },
                new Setter { Property = Button.PaddingProperty, Value = new Thickness(16, 12) },
                new Setter { Property = Button.CornerRadiusProperty, Value = 12 },
                new Setter { Property = Button.FontSizeProperty, Value = 16d },
                new Setter { Property = Button.BackgroundColorProperty, Value = Color.FromArgb("#1D5FE8") },
                new Setter { Property = Button.TextColorProperty, Value = Colors.White }
            }
        });
        resources.Add(new Style(typeof(Entry))
        {
            Setters =
            {
                new Setter { Property = VisualElement.MinimumHeightRequestProperty, Value = 56d },
                new Setter { Property = Entry.FontSizeProperty, Value = 18d },
                new Setter { Property = Entry.TextColorProperty, Value = Color.FromArgb("#101A2E") },
                new Setter { Property = Entry.PlaceholderColorProperty, Value = Color.FromArgb("#64748B") },
                new Setter { Property = Entry.BackgroundColorProperty, Value = Colors.White }
            }
        });
        resources.Add(new Style(typeof(Editor))
        {
            Setters =
            {
                new Setter { Property = Editor.FontSizeProperty, Value = 17d },
                new Setter { Property = Editor.TextColorProperty, Value = Color.FromArgb("#101A2E") },
                new Setter { Property = Editor.PlaceholderColorProperty, Value = Color.FromArgb("#64748B") },
                new Setter { Property = Editor.BackgroundColorProperty, Value = Colors.White }
            }
        });
        resources.Add(new Style(typeof(Border))
        {
            Setters =
            {
                new Setter { Property = Border.StrokeProperty, Value = new SolidColorBrush(Color.FromArgb("#D7E2F4")) },
                new Setter { Property = Border.StrokeThicknessProperty, Value = 1d },
                new Setter { Property = Border.StrokeShapeProperty, Value = new RoundRectangle { CornerRadius = 14 } }
            }
        });
        resources.Add(new Style(typeof(ActivityIndicator))
        {
            Setters =
            {
                new Setter { Property = ActivityIndicator.ColorProperty, Value = Color.FromArgb("#1D5FE8") },
                new Setter { Property = VisualElement.MinimumHeightRequestProperty, Value = 32d }
            }
        });
        return resources;
    }
}

public class PrototypePage : ContentPage
{
    public PrototypePage()
    {
        Resources = PrototypeTheme.Create();
        BackgroundColor = Color.FromArgb("#F5F8FF");
        // MAUI 10 insets for system bars and soft keyboard; scoped to this page.
        SafeAreaEdges = SafeAreaEdges.All;
    }
}
