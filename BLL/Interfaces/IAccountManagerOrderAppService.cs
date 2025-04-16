using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Entities.Models;
using Entities.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using X.PagedList;

namespace  DAL.Interfaces;

public interface IAccountManagerOrderAppService
{
    List<OrderSectionVM> GetOrderSection();

    List<WaitingListVM> GetAllWaitingListCustomer(int sectionId);

    public int GetCustomerIDByName(string email);

    List<MenuCategoryVM> SetOrderItemData(List<MenuCategoryVM> ItemDetail);

    List<CustomerTableVM> GetTablesByCustomerId(int customerId);

    CustomerVM GetCustomerDetails(int customerId);

    WaitingListVM GetWaitingData(int waitingId);

    WaitingListVM GetWaitingDataByEmail(string email);

    Customer IsCustomer(string email);

    bool AddOrUpdateCustomer([FromBody] JsonObject customerdata);

    bool EditCustomer(CustomerVM customerdata);

    bool AddInWaitingList([FromBody] JsonObject customerdata);

    List<OrderVM> GetOrderCategoryItem(int categoryId, string? status);

    bool UpdateOrderStatus(List<PrepareItemVM> prepare);

    bool AddToFavouriteItem(int ItemId);

    List<MenuCategoryVM> GetFavouriteItem();

    bool DeleteWaitingToken(int waitingId);

    bool AssignTable(int waitingId, int tableId);

    public MenuCategoryVM GetItemModifier(int ItemId);

}