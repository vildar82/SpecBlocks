﻿using System.Collections.Generic;
using System.Linq;
using AcadLib.Errors;

namespace SpecBlocks
{
    /// <summary>
    /// Группирование элементов в спецификации
    /// </summary>
    class SpecGroup
    {
        public string Name { get; private set; }
        /// <summary>
        /// Уникальные строки элементов таблицы - по ключевому свойству
        /// </summary>
        public List<SpecRecord> Records { get; private set; } = new List<SpecRecord>();

        public SpecGroup(string name)
        {
            Name = name;
        }

        public static List<SpecGroup> Grouping(SpecTable specTable)
        {
            List<SpecGroup> groups = new List<SpecGroup>();
            var itemsGroupBy = specTable.Items.GroupBy(i => i.Group).OrderBy(g => g.Key);
            foreach (var itemGroup in itemsGroupBy)
            {
                SpecGroup group = new SpecGroup(itemGroup.Key);
                group.Calc(itemGroup, specTable);
                // проверка уникальности элементов в группе
                group.Check(specTable);
                groups.Add(group);
            }
            return groups;
        }

        public void Calc(IGrouping<string, SpecItem> itemGroup, SpecTable specTable)
        {
            // itemGroup - элементы одной группы.

            // Нужно сгруппировать по ключевому свойству
            //var uniqRecs = itemGroup.GroupBy(m => m.Key).OrderBy(m => m.Key, new AcadLib.Comparers.AlphanumComparator());

            //  Группировка по уникальным значениям каждого параметра
            var uniqRecs = itemGroup.GroupBy(m => m).OrderBy(m => m.Key.Key, new AcadLib.Comparers.AlphanumComparator());

            //var groups = piles.GroupBy(g => new { g.View, g.TopPileAfterBeat, g.TopPileAfterCut, g.BottomGrillage, g.PilePike })
            //                    .OrderBy(g => g.Key.View, AcadLib.Comparers.AlphanumComparator.New);
                        
            foreach (var urec in uniqRecs)
            {                
                SpecRecord rec = new SpecRecord(urec.Key.Key, urec.ToList(), specTable);
                Records.Add(rec);

                // Добавление элементов определенных групп в инспектор для показа пользователю                
                foreach (var item in urec)
                {                    
                        Inspector.AddError($"{item.BlName} {specTable.SpecOptions.KeyPropName}={item.Key}", item.IdBlRef,
                            icon: System.Drawing.SystemIcons.Information);                                         
                }                
            }

            // Дублирование марки
            var errRecsDublKey = uniqRecs.GroupBy(g => g.Key.Key).Where(w=>w.Skip(1).Any());            
            foreach (var errRecDublKey in errRecsDublKey)
            {
                int i = 0;                
                foreach (var items in errRecDublKey)
                {
                    i++;
                    foreach (var rec in items)
                    {
                        Inspector.AddError($"Дублирование марки в блоке {rec.BlName} {specTable.SpecOptions.KeyPropName}='{rec.Key}'-{i}, такая марка уже определена с другими параметрами блока.", rec.IdBlRef,
                        icon: System.Drawing.SystemIcons.Warning);
                    }                    
                }                
            }            
        }

        /// <summary>
        /// Проверка группы
        /// </summary>
        public void Check(SpecTable specTable)
        {
            Records.ForEach(r => r.CheckRecords(specTable));
        }
    }
}