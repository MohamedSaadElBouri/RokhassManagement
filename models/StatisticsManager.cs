using System;
using System.Collections.Generic;
using System.Linq;

namespace RakhesApp.Models
{
    public class StatisticsManager
    {
        private List<LicenseRecord> _records;
        
        public StatisticsManager()
        {
            _records = new List<LicenseRecord>();
        }

        // إحصائيات الربع الحالي (الأشهر 7، 8، 9)
        public Dictionary<LicenseType, int> GetQuarterlyStatistics()
        {
            var currentDate = DateTime.Now;
            var quarterStart = new DateTime(currentDate.Year, 7, 1);
            var quarterEnd = new DateTime(currentDate.Year, 9, 30);

            // إذا تجاوزنا 1 سبتمبر، نبدأ ربع جديد
            if (currentDate > new DateTime(currentDate.Year, 9, 1))
            {
                quarterStart = new DateTime(currentDate.Year, 10, 1);
                quarterEnd = new DateTime(currentDate.Year, 12, 31);
            }

            var quarterlyRecords = _records.Where(r => 
                r.DateAdded >= quarterStart && 
                r.DateAdded <= quarterEnd && 
                r.IsActive).ToList();

            return CalculateStatistics(quarterlyRecords);
        }

        // إحصائيات السنة الحالية
        public Dictionary<LicenseType, int> GetYearlyStatistics()
        {
            var currentYear = DateTime.Now.Year;
            var yearlyRecords = _records.Where(r => 
                r.DateAdded.Year == currentYear && 
                r.IsActive).ToList();

            return CalculateStatistics(yearlyRecords);
        }

        private Dictionary<LicenseType, int> CalculateStatistics(List<LicenseRecord> records)
        {
            var stats = new Dictionary<LicenseType, int>();
            
            foreach (LicenseType type in Enum.GetValues(typeof(LicenseType)))
            {
                stats[type] = records.Count(r => r.Type == type);
            }
            
            return stats;
        }

        public void AddRecord(LicenseRecord record)
        {
            record.Id = _records.Count + 1;
            record.DateAdded = DateTime.Now;
            record.IsActive = true;
            _records.Add(record);
        }

        public void CancelRecord(int recordId)
        {
            var record = _records.FirstOrDefault(r => r.Id == recordId);
            if (record != null)
            {
                record.IsActive = false;
            }
        }

        public List<LicenseRecord> GetAllRecords()
        {
            return _records.ToList();
        }
    }
}
