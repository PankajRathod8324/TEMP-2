using System.Threading.Tasks;
using Entities.Models;
using Entities.ViewModel;
using Microsoft.AspNetCore.Http;
using X.PagedList;

namespace  DAL.Interfaces;
public interface ITaxService
{
    List<Taxis> GetAllTaxes();
    List<TaxType> GetAllTaxTypes();
    IPagedList<TaxVM> getFilteredTaxes(UserFilterOptions filterOptions);

    Taxis GetTaxById(int taxId);

    void AddTax(Taxis tax);
    bool UpdateTax(Taxis tax);
    void DeleteTax(Taxis tax);
}