using Entity.Domain.Models.ModelBase;

namespace Entity.Domain.Models.Implements.AdministrationSystem
{
    public class SystemParameter : BaseModel
    {
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;

        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
    }
}
