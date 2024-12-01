using System.Text;
using Microsoft.JSInterop;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components;
using Blazor_ToDo_App.Data;
using Blazored.LocalStorage;
using Blazored.SessionStorage;

namespace Blazor_ToDo_App.Components.Pages;

public partial class TodoList {
    private List<string> todoList = null!;
    private string newEntryValue = string.Empty;
    private DateTime LastListRetrieval;
    private UsersContext? _usersContext;

    [Inject] IJSRuntime JS { get; set; } = null!;

    [Inject] IDbContextFactory<UsersContext> UsersCF { get; set; } = null!;

    [Inject] ILocalStorageService LocalStorage { get; set; } = null!;
    [Inject] ISessionStorageService SessionStorage { get; set; } = null!;

    private async void LogToBrowserConsole(object message) {
        await JS.InvokeVoidAsync("console.log", message);
    }

    private async void AddToList() {
        if (string.IsNullOrWhiteSpace(newEntryValue)) return;

        todoList.Add(newEntryValue);
        newEntryValue = "";
        OnEntryChange();
        await UpdateUserList();
    }

    private async void RemoveFromList(int index) {
        todoList.RemoveAt(index);
        await UpdateUserList();
    }

    private async void OnEntryChange() {
        await SessionStorage.SetItemAsync<string>("entry", newEntryValue);
    }

    private async Task DownloadToFile() {
        byte[] todoListBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, todoList));

        await JS.InvokeVoidAsync(
            "downloadFileFromStream",
            "ToDo List.txt",
            new DotNetStreamReference(new MemoryStream(todoListBytes))
        );
    }

    #region Database stuff

    private async Task<User> GetUserByID(Guid Id) {
        _usersContext ??= await UsersCF.CreateDbContextAsync();
        var tUsers = await _usersContext.Users!.ToListAsync();
        var tUser = tUsers.FirstOrDefault(x => x.Id == Id)!;
        return tUsers.FirstOrDefault(x => x.Id == Id)!;
    }

    private async Task AddUser(User user) {
        _usersContext ??= await UsersCF.CreateDbContextAsync();
        _usersContext.Users!.Add(user);
        await _usersContext!.SaveChangesAsync();
    }

    private async Task UpdateUserList() {
        await SessionStorage.SetItemAsync<List<string>>("todoList", todoList);

        _usersContext ??= await UsersCF.CreateDbContextAsync();
        var tUserID = await LocalStorage.GetItemAsync<Guid>("localUserID");

        var tUser = await GetUserByID(tUserID);

        if (tUser != null) {
            tUser.TodoList = todoList;
            _usersContext.Users!.Update(tUser);
            await _usersContext.SaveChangesAsync();
        }
    }
    #endregion

    private async Task GenerateNewUser() {
        var tGUID = Guid.NewGuid();
        todoList = [];
        var tUser = new User {
            Id = tGUID,
            TodoList = todoList
        };
        await AddUser(tUser);
        await LocalStorage.SetItemAsync<Guid>("localUserID", tGUID);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (firstRender) {

            LastListRetrieval = await SessionStorage.GetItemAsync<DateTime>("LastRetrieval");

            //LogToBrowserConsole(LastListRetrieval);

            var tUserID = await LocalStorage.GetItemAsync<Guid>("localUserID");

            if (tUserID == Guid.Empty) {
                await GenerateNewUser();

            } else {

                var tUser = await GetUserByID(tUserID);
                if (tUser == null) {
                    //User seems to have been deleted from database, generate new
                    await GenerateNewUser();
                }
            }

            //Check if old data or not
            if (LastListRetrieval == DateTime.MinValue || (DateTime.Now - LastListRetrieval).TotalSeconds > 60) {
                // Either old data or not fetched yet, re-fetch

                var tUser = await GetUserByID(tUserID);
                if (tUser != null) {
                    todoList = tUser.TodoList;
                }
                await SessionStorage.SetItemAsync<List<string>>("todoList", todoList);
                LastListRetrieval = DateTime.Now;
                await SessionStorage.SetItemAsync<DateTime>("LastRetrieval", LastListRetrieval);

            } else {
                var tList = await SessionStorage.GetItemAsync<List<string>>("todoList");
                if (tList != null) {
                    todoList = tList;
                }
            }

            newEntryValue = await SessionStorage.GetItemAsync<string>("entry") ?? "";

            StateHasChanged();
        }
    }
}
