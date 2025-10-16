using Entity.Domain.Models.ModelBase;

namespace Entity.Domain.Models.Implements.Business
{
    public class Clause : BaseModelGeneric
    {
        public string Description { get; set; } = null!;
        public ICollection<ContractClause> ContractUsages { get; set; } = new List<ContractClause>();
    }
}
