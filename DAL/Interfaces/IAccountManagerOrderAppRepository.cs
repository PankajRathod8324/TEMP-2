using System.Threading.Tasks;
using Entities.Models;
using Entities.ViewModel;
using Microsoft.AspNetCore.Http;
using X.PagedList;

namespace  DAL.Interfaces;
public interface IAccountManagerOrderAppRepository
{
    List<OrderSectionVM> GetOrderSection();

    List<WaitingListVM> GetAllWaitingListCustomer(int sectionId);

    List<CustomerTable> GetTablesByCustomerId(int customerId);

    Customer GetCustomer(int customerId);

    OrderItem GetOrderItem(int orderId, int itemId);

    public int GetCustomerIDByName(string email);

    bool EditCustomer(Customer customerdata);

    WaitingListVM GetWaitingData(int waitingId);

    WaitingListVM GetWaitingDataByEmail(string email);
    Customer IsCustomer(string email);

    WaitingList IsInWaitingList(string email);

    WaitingList GetWaitingListBySectionId(int sectionId);

    void AddInWaitingList(WaitingList customerTable);

    void DeleteCustomerFromWaitingList(string email);

    List<OrderVM> GetOrderCategoryItem(int categoryId, string? status);

    bool PrepareItem(List<PrepareItemVM> prepareItems);

    bool AddToFavouriteItem(Favourite favouriteItem);
    List<MenuCategoryVM> GetFavouriteItems();

     bool DeleteWaitingToken(int waitingId);
}