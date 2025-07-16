using System;

namespace RakhesApp.Models
{
    public enum LicenseType
    {
        تصريح = 1,      // Permis
        الرخص = 2,      // Licences
        قرارات_التحويل = 3,  // Décisions de transfert
        قرارات_الالغاء = 4   // Décisions d'annulation
    }

    public class LicenseRecord
    {
        public int Id { get; set; }
        public LicenseType Type { get; set; }
        public DateTime DateAdded { get; set; }
        public bool IsActive { get; set; }
        public string Description { get; set; }
        public string ReferenceNumber { get; set; }
    }
}
