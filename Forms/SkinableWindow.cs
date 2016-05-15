using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ArtContentManager.Forms
{
    public abstract class SkinableWindow : Window
    {
        public SkinableWindow()
        {
            ApplySkinViaSetting();
        }

        public void ApplySkinViaSetting()
        {
            if (Static.DatabaseAgents.dbaSettings.Setting("CurrentSkinUri") != null)
            {
                Uri skinURI = new Uri(Static.DatabaseAgents.dbaSettings.Setting("CurrentSkinUri").Item1, UriKind.Relative);
                ApplySkin(skinURI);
            }
        }

        public void ApplySkin(Uri skinDictionaryUri)
        {
            // Load the ResourceDictionary into memory.
            ResourceDictionary skinDict = Application.LoadComponent(skinDictionaryUri) as ResourceDictionary;

            System.Collections.ObjectModel.Collection<ResourceDictionary> mergedDicts = base.Resources.MergedDictionaries;

            // Remove the existing skin dictionary, if one exists.
            // NOTE: In a real application, this logic might need
            // to be more complex, because there might be dictionaries
            // which should not be removed.
            if (mergedDicts.Count > 0)
                mergedDicts.Clear();

            // Apply the selected skin so that all elements in the
            // application will honor the new look and feel.
            mergedDicts.Add(skinDict);
        }
    }
}
