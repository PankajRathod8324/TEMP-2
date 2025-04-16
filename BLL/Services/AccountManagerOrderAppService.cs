using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using DAL.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Entities.ViewModel;
using X.PagedList.Extensions;
using X.PagedList;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Text.Json.Nodes;
using Azure.Core;
using Microsoft.AspNetCore.Http;

namespace BLL.Services;

public class AccountManagerOrderAppService : IAccountManagerOrderAppService
{
    private readonly ICustomerRepository _customerRepository;

    private readonly IOrderRepository _orderRepository;

    private readonly IMenuRepository _menuRepository;

    private readonly ITableAndSectionRepository _tableRepository;

    private readonly IAccountManagerOrderAppRepository _accountmanagerorderapprepository;

    private readonly IUserRepository _userRepository;



    private readonly IHttpContextAccessor _httpContextAccessor;

    public AccountManagerOrderAppService(ICustomerRepository customerRepository, IOrderRepository orderRepository, ITableAndSectionRepository tableRepository, IAccountManagerOrderAppRepository accountmanagerorderapprepository, IUserRepository userRepository, IHttpContextAccessor httpContextAccessor, IMenuRepository menuRepository)
    {
        _customerRepository = customerRepository;
        _orderRepository = orderRepository;
        _tableRepository = tableRepository;
        _accountmanagerorderapprepository = accountmanagerorderapprepository;
        _userRepository = userRepository;
        _httpContextAccessor = httpContextAccessor;
        _menuRepository = menuRepository;
    }



    public List<OrderSectionVM> GetOrderSection()
    {
        var sections = _accountmanagerorderapprepository.GetOrderSection();

        return sections;
    }

    public int GetCustomerIDByName(string email)
    {
        return _accountmanagerorderapprepository.GetCustomerIDByName(email);
    }

    public List<WaitingListVM> GetAllWaitingListCustomer(int sectionId)
    {
        return _accountmanagerorderapprepository.GetAllWaitingListCustomer(sectionId);
    }

    public List<CustomerTableVM> GetTablesByCustomerId(int customerId)
    {
        var customertable = _accountmanagerorderapprepository.GetTablesByCustomerId(customerId).Select(t => new CustomerTableVM
        {
            SectionName = _tableRepository.GetSectionNameByTableId((int)t.TableId),
            TableName = _tableRepository.GetTableNameByTableId((int)t.TableId)
        }).ToList();


        return customertable;
    }

    public WaitingListVM GetWaitingData(int waitingId)
    {
        return _accountmanagerorderapprepository.GetWaitingData(waitingId);
    }

    public WaitingListVM GetWaitingDataByEmail(string email)
    {
        return _accountmanagerorderapprepository.GetWaitingDataByEmail(email);
    }

    public CustomerVM GetCustomerDetails(int customerId)
    {
        var personaldata = _accountmanagerorderapprepository.GetCustomer(customerId);
        var finaldata = new CustomerVM
        {
            CustomerId = personaldata.CustomerId,
            Name = personaldata.Name,
            Email = personaldata.Email,
            Phone = personaldata.Phone,
            NoOfPerson = (int)personaldata.Noofperson
        };
        return finaldata;
    }

    public Customer IsCustomer(string email)
    {
        return _accountmanagerorderapprepository.IsCustomer(email);
    }

    public bool AddOrUpdateCustomer([FromBody] JsonObject customerdata)
    {

        // Extract customer details from JSON
        string email = (string)customerdata["email"];
        string name = (string)customerdata["Name"];
        string phone = (string)customerdata["Phone"];
        int noOfPerson = TryParseInt(customerdata["noOfPerson"]);
        List<Table> selectedTables = new List<Table>();

        if (customerdata.ContainsKey("SelectedTable") && customerdata["SelectedTable"] != null)
        {
            selectedTables = JsonConvert.DeserializeObject<List<Table>>(customerdata["SelectedTable"].ToString());
        }

        // Check if the customer already exists
        var existingCustomer = _accountmanagerorderapprepository.IsCustomer(email);

        if (existingCustomer != null)
        {
            // Customer exists -> Update NoOfPerson
            existingCustomer.Noofperson = noOfPerson;
            _customerRepository.UpdateCustomer(existingCustomer);

            // Add selected tables to the CustomerTable relation
            foreach (var table in selectedTables)
            {
                var customerTable = new CustomerTable
                {
                    CustomerId = existingCustomer.CustomerId,
                    TableId = table.TableId
                };
                var tablestatus = new Table
                {
                    TableId = table.TableId,
                    StatusId = 3,
                    OccupiedTime = DateTime.UtcNow
                };
                _tableRepository.UpdateTable(tablestatus);
                _customerRepository.AddCustomerTable(customerTable);
            }
        }
        else
        {
            // Customer does not exist -> Insert new customer
            var newCustomer = new Customer
            {
                Email = email,
                Name = name,
                Phone = phone,
                Noofperson = noOfPerson
            };
            _customerRepository.AddCustomer(newCustomer);
            _accountmanagerorderapprepository.DeleteCustomerFromWaitingList(email);

            // Add selected tables to the CustomerTable relation
            foreach (var table in selectedTables)
            {
                var customerTable = new CustomerTable
                {
                    CustomerId = newCustomer.CustomerId,
                    TableId = table.TableId
                };
                var tablestatus = new Table
                {
                    TableId = table.TableId,
                    StatusId = 3,
                    OccupiedTime = DateTime.UtcNow
                };
                _tableRepository.UpdateTable(tablestatus);
                _customerRepository.AddCustomerTable(customerTable);
            }
        }


        return true;

    }

    public bool AddInWaitingList([FromBody] JsonObject customerdata)
    {

        // Extract customer details from JSON
        string email = (string)customerdata["email"];
        string name = (string)customerdata["Name"];
        string phone = (string)customerdata["Phone"];
        int noOfPerson = TryParseInt(customerdata["noOfPerson"]);
        int sectionId = TryParseInt(customerdata["SectionId"]);

        // Customer does not exist -> Insert new customer
        var newCustomer = new WaitingList
        {
            Email = email,
            Name = name,
            Phone = phone,
            NoOfPerson = noOfPerson,
            SectionId = sectionId
        };
        _accountmanagerorderapprepository.AddInWaitingList(newCustomer);
        return true;
    }

    public bool AddToFavouriteItem(int ItemId)
    {
        string useremail = _httpContextAccessor.HttpContext.Request.Cookies["Email"];
        var user = _userRepository.GetUserByEmail(useremail);
        var favouriteItem = new Favourite
        {
            ItemId = ItemId,
            UserId = user.UserId,
        };

        _accountmanagerorderapprepository.AddToFavouriteItem(favouriteItem);
        return true;
    }

    public List<MenuCategoryVM> GetFavouriteItem()
    {
        return _accountmanagerorderapprepository.GetFavouriteItems();
    }

    public List<OrderVM> GetOrderCategoryItem(int categoryId, string? status)
    {
        var orderCategoryItems = _accountmanagerorderapprepository.GetOrderCategoryItem(categoryId, status);
        return orderCategoryItems;
    }

    public bool UpdateOrderStatus(List<PrepareItemVM> prepare)
    {
        _accountmanagerorderapprepository.PrepareItem(prepare);

        return true; // Return true if the operation is successful
    }

    public bool DeleteWaitingToken(int waitingId)
    {
        var waitingList = _accountmanagerorderapprepository.GetWaitingData(waitingId);
        if (waitingList != null)
        {
            _accountmanagerorderapprepository.DeleteWaitingToken(waitingId);
            return true;
        }
        return false;
    }

    public bool AssignTable(int waitingId, int tableId)
    {
        var waitingList = _accountmanagerorderapprepository.GetWaitingData(waitingId);
        var IsCustomer = _accountmanagerorderapprepository.IsCustomer(waitingList.Email);
        if (IsCustomer != null)
        {
            IsCustomer.Noofperson = waitingList.NoOfPerson;
            _customerRepository.UpdateCustomer(IsCustomer);

            var customerTable = new CustomerTable
            {
                CustomerId = IsCustomer.CustomerId,
                TableId = tableId
            };
            var tablestatus = new Table
            {
                TableId = tableId,
                StatusId = 3,
                OccupiedTime = DateTime.UtcNow
            };
            _tableRepository.UpdateTable(tablestatus);
            _customerRepository.AddCustomerTable(customerTable);
            _accountmanagerorderapprepository.DeleteCustomerFromWaitingList(waitingList.Email);

        }
        else
        {
            var customer = new Customer
            {
                Email = waitingList.Email,
                Name = waitingList.Name,
                Phone = waitingList.Phone,
                Noofperson = waitingList.NoOfPerson
            };
            _customerRepository.AddCustomer(customer);
            _accountmanagerorderapprepository.DeleteCustomerFromWaitingList(waitingList.Email);

            var customerTable = new CustomerTable
            {
                CustomerId = customer.CustomerId,
                TableId = tableId
            };
            var tablestatus = new Table
            {
                TableId = tableId,
                StatusId = 3,
                OccupiedTime = DateTime.UtcNow
            };
            _tableRepository.UpdateTable(tablestatus);
            _customerRepository.AddCustomerTable(customerTable);
        }

        return true;
    }

    public MenuCategoryVM GetItemModifier(int ItemId)
    {
        var item = _menuRepository.GetItemById(ItemId);
        var itemmodifiers = _menuRepository.GetItemModifier(ItemId);
        return new MenuCategoryVM
        {
            ItemId = ItemId,
            ItemName = _menuRepository.GetItemNameById(ItemId),
            ModifierGroupIds = itemmodifiers.Select(m => new ItemModifierVM
            {
                ModifierGroupName = _menuRepository.GetModifierGroupNameById(m.ModifierGroupId),
                MaxSelection = m.MaxSelection,
                MinSelection = m.MinSelection,
                MenuModifiers = _menuRepository.GetModifiersByModifierGroupId(m.ModifierGroupId).Select(mod => new ModifierVM
                {
                    ModifierId = mod.ModifierId,
                    ModifierName = mod.ModifierName,
                    ModifierRate = (decimal)mod.ModifierRate,
                }).ToList()
            }).ToList()
        };


    }

    public List<MenuCategoryVM> SetOrderItemData(List<MenuCategoryVM> ItemDetail)
    {
        var finalitemdata = ItemDetail.Select(item => new MenuCategoryVM
        {
            ItemId = item.ItemId,
            ItemName = _menuRepository.GetItemById(item.ItemId).ItemName,
            Rate = _menuRepository.GetItemById(item.ItemId).Rate,
            MenuItemModifier = item.MenuItemModifier.Select(mod => new MenuModifierGroupVM
            {
                ModifierId = mod.ModifierId,
                ModifierName = _menuRepository.GetModifierNameById(mod.ModifierId),
                ModifierRate = _menuRepository.GetModifierById(mod.ModifierId).ModifierRate,
               
            }).ToList(),

        }).ToList();
        return finalitemdata;
    }

    public bool EditCustomer(CustomerVM customerdata)
    {
        var existingCustomer = _accountmanagerorderapprepository.GetCustomer(customerdata.CustomerId);
        if (existingCustomer != null)
        {
            existingCustomer.Name = customerdata.Name;
            existingCustomer.Email = customerdata.Email;
            existingCustomer.Noofperson = customerdata.NoOfPerson;
            existingCustomer.Phone = customerdata.Phone;
        }


        return _accountmanagerorderapprepository.EditCustomer(existingCustomer);
    }

    private static int TryParseInt(object value)
    {
        return int.TryParse(value?.ToString(), out int result) ? result : 0;
    }



}
