using DAL.Interfaces;
using Entities.Models;
using Entities.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repository;

public class AccountManagerOrderAppRepository : IAccountManagerOrderAppRepository
{
    private readonly PizzaShopContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly IMenuRepository _menuRepository;
    public AccountManagerOrderAppRepository(PizzaShopContext context, IMenuRepository menuRepository, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _menuRepository = menuRepository;
        _httpContextAccessor = httpContextAccessor;
    }


    public List<OrderSectionVM> GetOrderSection()
    {
        var sectionData = _context.Sections.Select(section => new OrderSectionVM
        {
            SectionId = section.SectionId,
            SectionName = section.SectionName,
            Available = section.Tables.Count(t => t.Status.StatusName == "Available"),
            Assigned = section.Tables.Count(t => t.Status.StatusName == "Assigned"),
            Running = section.Tables.Count(t => t.Status.StatusName == "Running"),
            Reserved = section.Tables.Count(t => t.Status.StatusName == "Reserved"),
            Table = section.Tables.Select(table => new OrderTableVM
            {
                SectionId = (int)table.SectionId,
                TableId = table.TableId,
                TableName = table.TableName,
                Status = table.Status.StatusName,
                Capacity = table.Capacity,
                OccuipiedTime = table.OccupiedTime.HasValue ? (DateTime.UtcNow - table.OccupiedTime.Value) : TimeSpan.Zero,
                // NoOfPerson = _context.CustomerTables.Where(t => t.TableId)
                OrderTable = table.OrderTables.Select(ordertable => new OrderTableVM
                {
                    OrderId = ordertable.OrderId,
                    Order = _context.Orders.Where(order => order.OrderId == ordertable.OrderId).Select(order => new OrderVM
                    {
                        OrderAmount = order.TotalAmount,
                        CustomerId = order.CustomerId
                    }).ToList(),
                }).ToList(),
            }).ToList(),


        }).ToList();
        return sectionData;
    }

    public List<CustomerTable> GetTablesByCustomerId(int customerId)
    {
        return _context.CustomerTables.Where(c => c.CustomerId == customerId).ToList();
    }

    public OrderItem GetOrderItem(int orderId, int itemId)
    {
        var orderItem = _context.OrderItems
            .Include(oi => oi.OrderModifiers)
            .FirstOrDefault(oi => oi.OrderId == orderId && oi.ItemId == itemId);

        return orderItem;
    }
    public int GetCustomerIDByName(string email)
    {
        var userId = (from user in _context.Customers
                        where user.Email == email
                        select user.CustomerId).FirstOrDefault();
        return userId;
    }

    public List<WaitingListVM> GetAllWaitingListCustomer(int sectionId)
    {
        var waitingListData = _context.WaitingLists
            .Where(sectionId == 0 ? temp => temp.IsDeleted == false : temp => temp.SectionId == sectionId && temp.IsDeleted == false)
            .Select(temp => new WaitingListVM
            {
                SectionId = temp.SectionId,
                Name = temp.Name,
                WaitingListId = temp.WaitingListId,
                Email = temp.Email,
                NoOfPerson = temp.NoOfPerson,
                Phone = temp.Phone,
            }).ToList();
        return waitingListData;
    }

    public WaitingListVM GetWaitingData(int waitingId)
    {
        var waitingData = _context.WaitingLists.Where(waiting => waiting.WaitingListId == waitingId).FirstOrDefault();
        var waitingvm = new WaitingListVM
        {
            SectionId = waitingData.SectionId,
            Name = waitingData.Name,
            WaitingListId = waitingData.WaitingListId,
            Email = waitingData.Email,
            NoOfPerson = waitingData.NoOfPerson,
            Phone = waitingData.Phone,
        };

        return waitingvm;
    }

    public WaitingListVM GetWaitingDataByEmail(string email)
    {
        var waitingData = _context.Customers.Where(waiting => waiting.Email == email).FirstOrDefault();
        if (email == null)
        {
            return null;
        }
        var waitingvm = new WaitingListVM
        {
            SectionId = _context.Tables.Where(t => t.TableId == waitingData.TableId).FirstOrDefault().SectionId,
            Name = waitingData.Name,
            Email = waitingData.Email,
            NoOfPerson = (int)waitingData.Noofperson,
            Phone = waitingData.Phone,
        };

        return waitingvm;
    }
    public WaitingList GetWaitingListBySectionId(int sectionId)
    {
        var waitingList = _context.WaitingLists
            .Where(w => w.SectionId == sectionId && w.IsDeleted == false)
            .ToList();

        return waitingList.FirstOrDefault();
    }

    public Customer IsCustomer(string email)
    {
        return _context.Customers.FirstOrDefault(c => c.Email == email);
    }

     public Customer GetCustomer(int customerId)
    {
        return _context.Customers.FirstOrDefault(c => c.CustomerId == customerId);
    }

    public WaitingList IsInWaitingList(string email)
    {
        return _context.WaitingLists.FirstOrDefault(c => c.Email == email);
    }

    public void AddInWaitingList(WaitingList customerTable)
    {
        _context.WaitingLists.Add(customerTable);
        Save();
    }

    public void DeleteCustomerFromWaitingList(string email)
    {
        var existingItem = _context.WaitingLists.FirstOrDefault(m => m.Email == email);
        existingItem.IsDeleted = true;
        Save();
    }

    public List<OrderVM> GetOrderCategoryItem(int categoryId, string? status)
    {
        var ordersQuery = _context.Orders.AsQueryable();

        if (categoryId != 0)
        {
            ordersQuery = ordersQuery.Where(o => _context.OrderItems.Any(oi => oi.OrderId == o.OrderId && oi.ItemId.HasValue
                                                                               && _context.MenuItems.Any(mi => mi.ItemId == oi.ItemId
                                                                                                               && mi.CategoryId == categoryId)));
        }
        else
        {
            ordersQuery = ordersQuery.Where(o => _context.OrderItems.Any(oi => oi.OrderId == o.OrderId && oi.ItemId.HasValue
                                                                               && _context.MenuItems.Any(mi => mi.ItemId == oi.ItemId)));
        }

        // Fetch orders and related data into memory
        var orders = ordersQuery
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.OrderModifiers)
            .ToList();

        var orderVm = orders.Select(o => new OrderVM
        {
            OrderId = o.OrderId,
            OccupiedTime = DateTime.Now - o.CreatedAt,
            OrderInstruction = o.OrderInstructions,
            OrderItems = o.OrderItems
                .Where(oi => oi.ItemId.HasValue && ((status == "In Progress" && (oi.Quantity - oi.Readyitemquantity) > 0) ||
                                  (status != "In Progress" && oi.Readyitemquantity > 0)) && _context.MenuItems.Any(mi => mi.ItemId == oi.ItemId))
                .SelectMany(oi => new List<OrderItemVM>
                {
                new OrderItemVM
                {
                    ItemId = (int)oi.ItemId,
                    ItemName = _context.MenuItems.Where(mi => mi.ItemId == oi.ItemId).Select(mi => mi.ItemName).FirstOrDefault(),
                    Price = oi.Rate,
                    Quantity = (status == "In Progress") ? oi.Quantity - oi.Readyitemquantity : oi.Readyitemquantity,
                    ItemInstructions = oi.ItemInstructions,
                    Status = status,
                    Modifiers = oi.OrderModifiers.Select(om => new OrderModifierVM
                    {
                        ModifierId = (int)om.ModifierId,
                        ModifierName = _context.MenuModifiers.Where(mod => mod.ModifierId == om.ModifierId).Select(mod => mod.ModifierName).FirstOrDefault(),
                        ModifierRate = om.Rate,
                        Quantity = om.Quantity
                    }).ToList()
                }

                }).ToList(),
            OrderTables = _context.OrderTables.Where(r => r.OrderId == o.OrderId).Select(ot => new OrderTableVM
            {
                TableId = (int)ot.TableId,
                TableName = ot.TableId.HasValue ? (from table in _context.Tables
                                                   where table.TableId == ot.TableId
                                                   select table.TableName).FirstOrDefault() : "No Table",
                SectionName = ot.TableId.HasValue ? (from table in _context.Tables
                                                     join section in _context.Sections on table.SectionId equals section.SectionId
                                                     where table.TableId == ot.TableId
                                                     select section.SectionName).FirstOrDefault() : "No Section"
            }).ToList(),

        }).Where(o => o.OrderItems.Count > 0).ToList();

        return orderVm;
    }

    public bool PrepareItem(List<PrepareItemVM> prepareItems)
    {
        foreach (var item in prepareItems)
        {
            var orderItem = _context.OrderItems
                .FirstOrDefault(x => x.OrderId == item.OrderId && x.ItemId == item.ItemId);
            if (item.Status == "In Progress")
            {
                if (orderItem != null)
                {
                    orderItem.Readyitemquantity += item.Quantity;
                }
            }
            else
            {
                if (orderItem != null)
                {
                    orderItem.Readyitemquantity -= item.Quantity;
                }
            }
        }

        _context.SaveChanges();

        foreach (var item in prepareItems)
        {
            var orderItem = _context.OrderItems
                .FirstOrDefault(x => x.OrderId == item.OrderId && x.ItemId == item.ItemId);


            if (orderItem.Quantity == orderItem.Readyitemquantity)
            {
                orderItem.Status = "Ready";
            }
            else
            {
                orderItem.Status = "In Progress";
            }
        }
        _context.SaveChanges();



        return true;
    }

    public List<MenuCategoryVM> GetFavouriteItems()
    {
        string useremail = _httpContextAccessor.HttpContext.Request.Cookies["Email"];
        var user = _context.Users.FirstOrDefault(u => u.Email == useremail);
        var favouriteItems = _context.Favourites
            .Where(f => f.UserId == user.UserId)
            .Select(f => f.ItemId)
            .ToList();

        var menuItems = _context.MenuItems
            .Where(m => favouriteItems.Contains(m.ItemId))
            .ToList();

        var itemvm = menuItems.Select(item => new MenuCategoryVM
        {
            ItemId = item.ItemId,
            ItemName = item.ItemName,
            UnitId = item.UnitId,
            CategoryId = item.CategoryId,
            ItemtypeId = item.ItemtypeId,
            Rate = item.Rate,
            Quantity = item.Quantity,
            IsAvailable = (bool)item.IsAvailable,
            TaxDefault = item.TaxDefault,
            TaxPercentage = item.TaxPercentage,
            ShortCode = item.ShortCode,
            Description = item.Description,
            ItemPhoto = item.CategoryPhoto,
            IsFavourite = _menuRepository.IsItemFavourite(item.ItemId, user.UserId) ? true : false,
        }).ToList();

        return itemvm;
    }

    public bool AddToFavouriteItem(Favourite favouriteItem)
    {
        var existingItem = _context.Favourites.FirstOrDefault(m => m.ItemId == favouriteItem.ItemId && m.UserId == favouriteItem.UserId);
        if (existingItem == null)
        {
            _context.Favourites.Add(favouriteItem);
            Save();
            return true;
        }
        else
        {
            _context.Favourites.Remove(existingItem);
            Save();
            return false;
        }
    }

    public bool DeleteWaitingToken(int waitingId)
    {
        var waitingList = _context.WaitingLists.FirstOrDefault(w => w.WaitingListId == waitingId);
        if (waitingList != null)
        {
            waitingList.IsDeleted = true;
            Save();
            return true;
        }
        return false;
    }

    public bool EditCustomer(Customer customerdata)
    {
        Save();
        return true;
    }
    public void Save()
    {
        _context.SaveChanges();
    }


}