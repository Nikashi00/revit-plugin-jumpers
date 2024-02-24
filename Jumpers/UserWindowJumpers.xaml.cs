using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace Jumpers
{
    /// <summary>
    /// Логика взаимодействия для UserWindowJumpers.xaml
    /// </summary>
    public partial class UserWindowJumpers : Window
    {
        UIDocument _uiDoc;
        Document _doc;
        View view;
        FamilySymbol jumperSymbol; //типоразмер перемычки
        List<FamilyInstance> selectedTypeOpenings; //список экземпляров выбранного семейства проема
        List<Family> allOpenings; //список всех семейств проемов (двери и окна)
        List<FamilyInstance> selectedOpenings;
        List<Element> wallTypes; //список типоразмеров стен
        IList<Reference> refs;
        bool filterSelect = true; //переключатель выбора элементов (да - видимые на виде, нет - все в проекте)

        public UserWindowJumpers(Document doc, UIDocument uiDoc)
        {
            InitializeComponent();
            _uiDoc = uiDoc;
            _doc = doc;
            view = _doc.ActiveView;

            int[] correct = check();
            if ((correct[0] != 0) | (correct[1] != 0)) 
            {
                MessageBox.Show("Корректировка размещенных перемычек:\n\n" + "Количество удаленных перемычек: " + correct[0]+
                    "\nКоличество измененных перемычек: " + correct[1]); 
            }

            //добавление семейства перемычек и типоразмеров в выпадающие списки
            //семейство
            Combobox_JumperFamily.Items.Add("Перемычка");
            //получение всех семейств в проекте
            Family[] families = new FilteredElementCollector(_doc).OfClass(typeof(Family)).Cast<Family>().ToArray();

            allOpenings = new List<Family>(); //все проемы (двери и окна)
            selectedTypeOpenings = new List<FamilyInstance>(); //список экземпляров выбранного семейства проема
            wallTypes = new List<Element>(); //список типоразмеров стен
            selectedOpenings = new List<FamilyInstance>();

            //выбор из всех семейств - семейств категории "Двери"
            foreach (Family fs in families)
            {
                if (fs.FamilyCategory.Name == "Двери")
                {
                    Combobox_OpeningType.Items.Add(fs.Name);
                    allOpenings.Add(fs);
                }
            }
            //выбор из всех семейств - семейств категории "Окна"
            foreach (Family fs in families)
            {
                if (fs.FamilyCategory.Name == "Окна")
                {
                    Combobox_OpeningType.Items.Add(fs.Name);
                    allOpenings.Add(fs);
                }
            }

            Family fj = null;
            foreach (Family fs in families)
            {
                if (fs.Name == "Перемычка")
                {
                    fj = fs; break;
                }
            }

            ISet<ElementId> idsSet = fj.GetFamilySymbolIds(); //интерфейс - ID типоразмеров выбранного семейства проема
            List<ElementId> ids = idsSet.ToList();
            foreach (ElementId i in ids)
            {
                Combobox_JumperTypes.Items.Add(_doc.GetElement(i).Name);
            }
        }

        public class openingsSF : ISelectionFilter
        {
            public bool AllowElement(Element el)
            {
                if ((el.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Doors) || 
                    (el.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Windows)) return true;
                else return false;
            }
            public bool AllowReference(Reference refer, XYZ point)
            {
                return true;
            }
        }

        //функция - при нажатии кнопки "Разместить перемычки"
        private void placeJumpers(object sender, RoutedEventArgs e)
        {
            using (Transaction transaction = new Transaction(_doc, "Insert doors"))
            {
                if (Combobox_JumperFamily.SelectedItem == null) MessageBox.Show("Выберите семейство перемычки");
                else if (Combobox_JumperTypes.SelectedItem == null) MessageBox.Show("Выберите типоразмер перемычки");
                else if ((Combobox_OpeningType.SelectedItem == null) & (Rbtn3.IsChecked == false)) MessageBox.Show("Выберите семейство проема");
                else if ((Combobox_WallTypes.SelectedItem == null) & (Rbtn3.IsChecked == false)) MessageBox.Show("Выберите типоразмер стены");
                else
                {
                    if (Rbtn3.IsChecked == true)
                    {
                        Close();
                        try
                        {
                            ISelectionFilter osf = new openingsSF();
                            refs = _uiDoc.Selection.PickObjects(ObjectType.Element, osf);
                            foreach (Reference r in refs)
                            {
                                selectedOpenings.Add(_doc.GetElement(r.ElementId) as FamilyInstance);
                            }
                        }
                        catch { }
                    }
                    else
                    {
                        //получение выбранных проемов
                        foreach (FamilyInstance fi in selectedTypeOpenings)
                        {
                            if (fi.Host.Name == Combobox_WallTypes.SelectedItem.ToString()) selectedOpenings.Add(fi);
                        }
                    }

                    //получение типоразмера перемычки для размещения
                    jumperSymbol = new FilteredElementCollector(_doc).OfClass(typeof(FamilySymbol))
                                                                           .OfCategory(BuiltInCategory.OST_GenericModel)
                                                                           .Cast<FamilySymbol>()
                                                                           .First(it => it.FamilyName == Combobox_JumperFamily.SelectedItem.ToString() && it.Name == Combobox_JumperTypes.SelectedItem.ToString());
                    //запуск расстановки перемычек
                    transaction.Start();

                    if (!jumperSymbol.IsActive)
                    {
                        jumperSymbol.Activate();
                    }

                    double wallThikness = 0;

                    foreach (var opening in selectedOpenings)
                    {
                        //Получение координат дверей и окон
                        LocationPoint openingLocation = opening.Location as LocationPoint;
                        XYZ openingCenter = openingLocation.Point;
                        double angle = openingLocation.Rotation;
                        double h = opening.LookupParameter(openingHeight.Text).AsDouble();
                        //Создание перемычки
                        Element jumper = _doc.Create.NewFamilyInstance(new XYZ(openingCenter.X, openingCenter.Y, openingCenter.Z + h), jumperSymbol, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                        //Поворот перемычки
                        LocationPoint jumperLocation = jumper.Location as LocationPoint;
                        Autodesk.Revit.DB.Line axis = Autodesk.Revit.DB.Line.CreateBound(openingCenter, new XYZ(openingCenter.X, openingCenter.Y, openingCenter.Z + 1));
                        jumperLocation.Rotate(axis, angle);
                        //Изменение ширины перемычки
                        wallThikness = _doc.GetElement(opening.Host.GetTypeId()).LookupParameter("Толщина").AsDouble();
                        jumper.LookupParameter("Ширина").Set(wallThikness);
                        //Изменение длины перемычки
                        double length = opening.LookupParameter(openingWidth.Text).AsDouble();
                        jumper.LookupParameter("Длина").Set(length);
                        if (length < UnitUtils.ConvertToInternalUnits(1500, UnitTypeId.Millimeters))
                        {
                            jumper.LookupParameter("Длина опирания 1").Set(UnitUtils.ConvertToInternalUnits(200, UnitTypeId.Millimeters));
                            jumper.LookupParameter("Длина опирания 2").Set(UnitUtils.ConvertToInternalUnits(200, UnitTypeId.Millimeters));
                        }
                        else if (length <= UnitUtils.ConvertToInternalUnits(3000, UnitTypeId.Millimeters))
                        {
                            jumper.LookupParameter("Длина опирания 1").Set(UnitUtils.ConvertToInternalUnits(250, UnitTypeId.Millimeters));
                            jumper.LookupParameter("Длина опирания 2").Set(UnitUtils.ConvertToInternalUnits(250, UnitTypeId.Millimeters));
                        }
                        else
                        {
                            jumper.LookupParameter("Длина опирания 1").Set(UnitUtils.ConvertToInternalUnits(300, UnitTypeId.Millimeters));
                            jumper.LookupParameter("Длина опирания 2").Set(UnitUtils.ConvertToInternalUnits(300, UnitTypeId.Millimeters));
                        }
                        //Запись информации о проеме в перемычку
                        jumper.LookupParameter("id проема").Set(opening.Id.IntegerValue);

                        //length = UnitUtils.ConvertFromInternalUnits(length, UnitTypeId.Meters);
                    }
                    transaction.Commit();
                    MessageBox.Show("Количество размещенных перемычек: " + selectedOpenings.Count.ToString());
                    Close(); //закрытие окна плагина
                }
            }
        }

        //функция - при выборе пункта "Видимые на виде"
        private void filterView(object sender, RoutedEventArgs e)
        {
            filterSelect = true;
            Combobox_OpeningType.IsEnabled = true;
            Combobox_WallTypes.IsEnabled = true;
        }

        //функция - при выборе пункта "Во всем проекте"
        private void filterAll(object sender, RoutedEventArgs e)
        {
            filterSelect = false;
            Combobox_OpeningType.IsEnabled = true;
            Combobox_WallTypes.IsEnabled = true;
        }

        private void rbtn3(object sender, RoutedEventArgs e)
        {
            Combobox_OpeningType.IsEnabled = false;
            Combobox_WallTypes.IsEnabled = false;
            //Combobox_OpeningType.Items.Clear();
            //Combobox_WallTypes.Items.Clear();
        }

        //функция - при выборе семейства проема. Находит типоразмеры стен, соответствующие экземплярам выбранного семейства
        private void findWallTypes(object sender, SelectionChangedEventArgs e)
        {
            //очищение списков, нужное при неоднократном изменении выбора
            Combobox_WallTypes.Items.Clear();
            selectedTypeOpenings.Clear();
            wallTypes.Clear();
            //создание переменных
            Family fam = null;
            List<ElementId> wallTypesIDs = new List<ElementId>(); //список ID типоразмеров стен
            //фильтр семейств проемов - из всех выбирается то, что выбрал пользователь
            foreach (Family f in allOpenings)
            {
                if (f.Name.ToString() == Combobox_OpeningType.SelectedItem.ToString()) { fam = f; break; }
            }

            ISet<ElementId> idsSet = fam.GetFamilySymbolIds(); //интерфейс - ID типоразмеров выбранного семейства проема
            List<ElementId> ids = idsSet.ToList(); //преобразование интерфейса в список
            IList<Element> temp = new List<Element>(); //временная переменная - список элементов

            //цикл для получения экземпляров выбранного семейства проема. Перебирает все типоразмеры семейства по их ID
            foreach (ElementId id in ids)
            {
                //создание фильтра по ID типоразмера
                FamilyInstanceFilter filter = new FamilyInstanceFilter(_doc, id);
                //получение элементов типоразмера (в зависимости от переключателя видимые на виде или все)
                if (filterSelect) temp = new FilteredElementCollector(_doc, view.Id).WherePasses(filter).ToElements();
                else temp = new FilteredElementCollector(_doc).WherePasses(filter).ToElements();
                //преобразование "элементов" в "экземпляры семейств"
                foreach (Element t in temp)
                {
                    selectedTypeOpenings.Add(t as FamilyInstance);
                }
            }

            //цикл для получения уникального списка ID типоразмеров стен, соответствующих экземплярам выбранного семейства проема
            foreach (FamilyInstance fi in selectedTypeOpenings)
            {
                ElementId ei = fi.Host.GetTypeId();
                if (wallTypesIDs.Contains(ei)) continue;
                else wallTypesIDs.Add(ei);
            }
            //получение типоразмеров стен по их ID, запись их в переменную и выпадающий список
            if (wallTypesIDs.Count > 0)
            {
                IList<Element> wtemp = new FilteredElementCollector(_doc, wallTypesIDs).WhereElementIsElementType().ToElements();
                foreach (Element wt in wtemp)
                {
                    wallTypes.Add(wt);
                    Combobox_WallTypes.Items.Add(wt.Name);
                }
            }
        }

        //функция - при выборе типоразмера стен. Выводит его толщину
        private void selectWallTypes(object sender, SelectionChangedEventArgs e)
        {
            double width = 0;
            if (Combobox_WallTypes.SelectedItem == null) Thickness.Text = "";
            else
            {
                foreach (Element wt in wallTypes)
                {
                    if (wt.Name == Combobox_WallTypes.SelectedItem.ToString()) { width = wt.LookupParameter("Толщина").AsDouble(); break; }
                }
                Thickness.Text = Math.Round(UnitUtils.ConvertFromInternalUnits(width, UnitTypeId.Millimeters)).ToString();
            }
        }

        //корректировка размещенных перемычек
        private int[] check()
        {
            int countDelete = 0;
            int countEdit = 0;
            int tempid = 0;
            double wt = 0;
            //получение всех семейств в проекте
            Family[] families = new FilteredElementCollector(_doc).OfClass(typeof(Family)).Cast<Family>().ToArray();

            Family familyJumper = null;
            List<FamilyInstance> jumpers = new List<FamilyInstance>();

            foreach (Family fs in families)
            {
                if (fs.Name == "Перемычка")
                {
                    familyJumper = fs; break;
                }
            }

            ISet<ElementId> idsSet = familyJumper.GetFamilySymbolIds(); //интерфейс - ID типоразмеров выбранного семейства проема
            List<ElementId> ids = idsSet.ToList(); //преобразование интерфейса в список
            IList<Element> temp = new List<Element>(); //временная переменная - список элементов

            //цикл для получения экземпляров выбранного семейства проема. Перебирает все типоразмеры семейства по их ID
            foreach (ElementId id in ids)
            {
                //создание фильтра по ID типоразмера
                FamilyInstanceFilter filter = new FamilyInstanceFilter(_doc, id);
                //получение элементов типоразмера (в зависимости от переключателя видимые на виде или все)
                temp = new FilteredElementCollector(_doc).WherePasses(filter).ToElements();
                //преобразование "элементов" в "экземпляры семейств"
                foreach (Element t in temp)
                {
                    jumpers.Add(t as FamilyInstance);
                }
            }

            Element opening = null;
            FamilyInstance inst = null;
            bool flag = false;

            using (Transaction transaction = new Transaction(_doc, "Correct"))
            {
                transaction.Start();

                foreach (FamilyInstance j in jumpers)
                {
                    tempid = j.LookupParameter("id проема").AsInteger();
                    flag = false;
                    if (tempid > 0)
                    {
                        opening = _doc.GetElement(new ElementId(tempid));
                        if (opening == null) { _doc.Delete(j.Id); countDelete++; }
                        else
                        {
                            LocationPoint openingLocation = opening.Location as LocationPoint;
                            XYZ openingCenter = openingLocation.Point;
                            LocationPoint jumperLocation = j.Location as LocationPoint;
                            XYZ jumperCenter = jumperLocation.Point;
                            if (Math.Round(openingCenter.X, 5) != Math.Round(jumperCenter.X, 5)) 
                            {
                                jumperLocation.Move(new XYZ((openingCenter.X - jumperCenter.X), 0, 0)); 
                                flag = true; 
                            }
                            if (Math.Round(openingCenter.Y, 5) != Math.Round(jumperCenter.Y, 5)) 
                            {
                                jumperLocation.Move(new XYZ(0, (openingCenter.Y - jumperCenter.Y), 0)); 
                                flag = true; 
                            }
                            inst = opening as FamilyInstance; 
                            wt = _doc.GetElement(inst.Host.GetTypeId()).LookupParameter("Толщина").AsDouble();
                            if (wt != j.LookupParameter("Ширина").AsDouble()) 
                            {
                                j.LookupParameter("Ширина").Set(wt); 
                                flag = true; 
                            }
                            if (flag) countEdit++;
                        }
                    }
                }
                transaction.Commit();
            }
            int[] result = {countDelete, countEdit};
            return result;
        }
    }
}
