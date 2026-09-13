namespace TutorClass.Mobile.PrototypeUI.Components;

public static class PrototypeElements
{
    public static Label Heading(string text) => new()
    {
        Text = text, FontSize = 22, FontAttributes = FontAttributes.Bold,
        Margin = new Thickness(0, 12, 0, 4)
    };

    public static Border Card(View content) => new()
    {
        Padding = 16, BackgroundColor = Colors.White, Content = content
    };

    public static Button Action(string text, string id, Func<Task> action)
    {
        var button = new Button { Text = text, AutomationId = id, HorizontalOptions = LayoutOptions.Fill };
        button.Clicked += async (_, _) => await action();
        return button;
    }
}
