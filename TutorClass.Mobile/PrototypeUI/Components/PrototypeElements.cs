namespace TutorClass.Mobile.PrototypeUI.Components;

public static class PrototypeElements
{
    public static Label Heading(string text) => new()
    {
        Text = text, FontSize = 20, FontAttributes = FontAttributes.Bold,
        TextColor = Color.FromArgb("#101A2E"), Margin = new Thickness(0, 14, 0, 4)
    };

    public static Border Card(View content) => new()
    {
        Padding = 16, BackgroundColor = Colors.White, Stroke = Color.FromArgb("#D7E2F4"),
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 18 }, Content = content
    };

    public static Button Action(string text, string id, Func<Task> action)
    {
        var button = new Button { Text = text, AutomationId = id, HorizontalOptions = LayoutOptions.Fill,
            BackgroundColor = Color.FromArgb("#E7EFFF"), TextColor = Color.FromArgb("#174BB8"),
            FontAttributes = FontAttributes.Bold, CornerRadius = 14, HeightRequest = 54,
            HorizontalTextAlignment = TextAlignment.Start };
        button.Clicked += async (_, _) => await action();
        return button;
    }
}
