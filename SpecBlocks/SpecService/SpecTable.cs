﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Errors;
using AcadLib.Jigs;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SpecBlocks.Options;

namespace SpecBlocks
{
   public class SpecTable
   {
      public SpecOptions SpecOptions { get; private set; }
      public List<SpecGroup> Groups { get; private set; } = new List<SpecGroup>();
      public Document Doc { get; private set; }= Application.DocumentManager.MdiActiveDocument;
      public SelectBlocks SelBlocks { get; private set; } = new SelectBlocks();
      public List<SpecItem> Items { get; private set; } = new List<SpecItem>();

      public SpecTable(SpecOptions options)
      {
         SpecOptions = options;
      }

      public void CreateTable()
      {
         // Выбор блоков
         SelBlocks.Select();

         using (var t = Doc.TransactionManager.StartTransaction())
         {
            try
            {
               // Фильтрация блоков
               Items = SpecItem.FilterSpecItems(this);
               // Группировка элементов
               Groups = SpecGroup.Grouping(this);
               
               // Создание таблицы
               Table table = getTable();
               // Вставка таблицы
               insertTable(table);
            }
            catch (Exception ex)
            {
               Logger.Log.Error(ex, "SpecTable.CreateTable().");
               Inspector.AddError(ex.Message);
            }

            t.Commit();
         }
      }

      private void insertTable(Table table)
      {
         Database db = Doc.Database;
         Editor ed = Doc.Editor;

         TableJig jigTable = new TableJig(table, 1/db.Cannoscale.Scale, "Вставка таблицы");
         if (ed.Drag(jigTable).Status == PromptStatus.OK)
         {            
            var cs = db.CurrentSpaceId.GetObject(OpenMode.ForWrite) as BlockTableRecord;
            cs.AppendEntity(table);
            db.TransactionManager.TopTransaction.AddNewlyCreatedDBObject(table, true);            
         }
      }

      private Table getTable()
      {
         Table table = new Table();
         table.SetDatabaseDefaults(Doc.Database);
         table.TableStyle = Doc.Database.GetTableStylePIK(); // если нет стиля ПИк в этом чертеже, то он скопируетс из шаблона, если он найдется         

         int rows = 2 + Groups.Count + Groups.Sum(g => g.Records.Count);
         table.SetSize(rows, SpecOptions.TableOptions.Columns.Count);

         for (int i = 0; i < table.Columns.Count; i++)
         {
            var specCol = SpecOptions.TableOptions.Columns[i];
            var col = table.Columns[i];
            col.Alignment = specCol.Aligment;
            col.Width = specCol.Width;
            col.Name = specCol.Name;

            var cellColName = table.Cells[1, i];
            cellColName.TextString = specCol.Name;
            cellColName.Alignment = CellAlignment.MiddleCenter;
         }

         // Название таблицы
         var rowTitle = table.Cells[0,0];
         rowTitle.Alignment = CellAlignment.MiddleCenter;
         rowTitle.TextHeight = 5;
         rowTitle.TextString = SpecOptions.TableOptions.Title;         

         // Строка заголовков столбцов
         var rowHeaders = table.Rows[1];
         rowHeaders.Height = 15;
         var lwBold = rowHeaders.Borders.Top.LineWeight;
         rowHeaders.Borders.Bottom.LineWeight = lwBold;

         int row = 2;
         foreach (var group in Groups)
         {
            table.Cells[row, 2].TextString = "{0}{1}{2}".f("{\\L",group.Name, "}");
            table.Cells[row, 2].Alignment = CellAlignment.MiddleCenter;            

            row++;
            foreach (var rec in group.Records)
            {
               for (int i = 0; i < table.Columns.Count; i++)
               {
                  var colVal = rec.ColumnsValue[i];
                  table.Cells[row, i].TextString = colVal.Value;
               }               
               row++;
            }            
         }
         var lastRow = table.Rows.Last();
         lastRow.Borders.Bottom.LineWeight = lwBold;

         table.GenerateLayout();
         return table;
      }
   }
}
