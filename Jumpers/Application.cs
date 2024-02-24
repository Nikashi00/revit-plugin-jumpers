using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Jumpers
{
    internal class Application : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            string assemblyLocation = Assembly.GetExecutingAssembly().Location,
                    iconsDirectoryPath = Path.GetDirectoryName(assemblyLocation) + @"\icons\",
                    tabName = "Перемычка";

            application.CreateRibbonTab(tabName); //для создания вкладки с выбранным именем

            {
                RibbonPanel panel = application.CreateRibbonPanel(tabName, "Размещение перемычек");//для создания панели на вкладке

               
                panel.AddItem(new PushButtonData(nameof(NewFamilyInstance_Jumpers), "Разместить\nперемычки", assemblyLocation, typeof(NewFamilyInstance_Jumpers).FullName)
                {
                    LargeImage = new BitmapImage(new Uri(iconsDirectoryPath + "Перемычки.png"))
                });



                return Result.Succeeded;
            }
        }
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
