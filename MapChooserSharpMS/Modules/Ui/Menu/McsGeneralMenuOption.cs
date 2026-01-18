using MapChooserSharpMS.Modules.Ui.Menu.Interfaces;

namespace MapChooserSharpMS.Modules.Ui.Menu;

public class McsGeneralMenuOption(string menuTitle, bool useTranslationKey) : IGeneralMenuOption
{
    public string MenuTitle { get; } = menuTitle;
    
    public bool UseTranslationKey { get; } = useTranslationKey;
}