using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAPITrainigCreationModel4
{
    [Transaction(TransactionMode.Manual)]

    public class Main : IExternalCommand
    {
        
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;


            List<Level> listLevel = new FilteredElementCollector(doc)
                     .OfClass(typeof(Level))
                     .OfType<Level>()
                     .ToList();

            Level level1 = listLevel
               .Where(x => x.Name.Equals("Уровень 1"))
               .FirstOrDefault();

            Level level2 = listLevel
              .Where(x => x.Name.Equals("Уровень 2"))
              .FirstOrDefault();

            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);
            double depth = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters);

            double dx = width / 2;
            double dy = depth / 2;

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            List<Wall> walls = new List<Wall>();

            ElementId id = doc.GetDefaultElementTypeId(ElementTypeGroup.RoofType);
            RoofType type = doc.GetElement(id) as RoofType;
            if (type == null)
            {
                TaskDialog.Show("Ошибка", "Not RoofType");
                return Result.Failed;
            }
            
            CurveArray curveArray = new CurveArray();
            curveArray.Append(Line.CreateBound(new XYZ(0, -10, 12), new XYZ(0, 0, 22)));
            curveArray.Append(Line.CreateBound(new XYZ(0, 0, 22), new XYZ(0, 10, 12)));



            Level level = doc.ActiveView.GenLevel;
            if (level == null)
            {
                TaskDialog.Show("Ошибка", "No es PlainView");
                return Result.Failed;
            }

            Transaction transaction = new Transaction(doc, "Создание стен");

            transaction.Start();
            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, level1.Id, false);
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
            }

            ReferencePlane plane = doc.Create.NewReferencePlane(new XYZ(0, 0, 0), new XYZ(0, 0, 20), new XYZ(0, 20, 0), doc.ActiveView);
            doc.Create.NewExtrusionRoof(curveArray, plane, level2, type, -20, 20);


            AddDoor(doc, level1, walls[0]);
            AddWindow(doc, level1, walls);
            //AddRoof(doc, level2, walls);

            transaction.Commit();
            return Result.Succeeded;
        }

      
        private void AddWindow(Document doc, Level level1, List<Wall> walls)
        {
            FamilySymbol windowType = new FilteredElementCollector(doc)
               .OfClass(typeof(FamilySymbol))
               .OfCategory(BuiltInCategory.OST_Windows)
               .OfType<FamilySymbol>()
               .Where(x => x.Name.Equals("0915 x 1830 мм"))
               .Where(x => x.FamilyName.Equals("Фиксированные"))
               .FirstOrDefault();

            for (int i = 1; i < 4; i++)
            {
                LocationCurve hostCurve = walls[i].Location as LocationCurve;
                XYZ point1 = hostCurve.Curve.GetEndPoint(0);
                XYZ point2 = hostCurve.Curve.GetEndPoint(1);
                XYZ point = (point1 + point2) / 2;
                double offset = UnitUtils.ConvertToInternalUnits(1100, UnitTypeId.Millimeters);
                point = new XYZ(point.X, point.Y, offset);
                doc.Create.NewFamilyInstance(point, windowType, walls[i], level1, StructuralType.NonStructural);

                if (!windowType.IsActive)
                    windowType.Activate();
            }

        }

        private void AddDoor(Document doc, Level level1, Wall wall)
        {
            FamilySymbol doorType= new FilteredElementCollector(doc)
             .OfClass(typeof(FamilySymbol))
             .OfCategory(BuiltInCategory.OST_Doors)
             .OfType<FamilySymbol>()
             .Where(x => x.Name.Equals("0915 x 2134 мм"))
             .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
             .FirstOrDefault();

            LocationCurve hostCurve=wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

            if (!doorType.IsActive)
                doorType.Activate();

            doc.Create.NewFamilyInstance(point, doorType, wall, level1, StructuralType.NonStructural);
        }
    }
}
