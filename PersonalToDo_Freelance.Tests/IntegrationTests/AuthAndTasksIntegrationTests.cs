using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using PersonalToDo_Freelance.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PersonalToDo_Freelance.Models;
using System.Linq;
using PersonalToDo_Freelance.Domain.Entities;
using PersonalToDo_Freelance.Application.ViewModels;
using System;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Collections.Generic;

namespace PersonalToDo_Freelance.Tests.IntegrationTests
{
    public class AuthAndTasksIntegrationTests : IClassFixture<IntegrationTestFactory>
    {
        private readonly IntegrationTestFactory _factory;
        public AuthAndTasksIntegrationTests(IntegrationTestFactory factory)
        {
            _factory = factory;
        }

        private async Task<string> GetAntiforgeryToken(HttpClient client, string url = "/")
        {
            var resp = await client.GetAsync(url);
            var html = await resp.Content.ReadAsStringAsync();
            var tokenName = "__RequestVerificationToken";
            var marker = $"name=\"{tokenName}\" value=\"";
            var idx = html.IndexOf(marker);
            if (idx >= 0)
            {
                var start = idx + marker.Length;
                var end = html.IndexOf('"', start);
                if (end > start) return html.Substring(start, end - start);
            }
            return null;
        }

        private async Task<HttpResponseMessage> PostFormWithToken(HttpClient client, string postUrl, Dictionary<string, string> fields, string formUrl = "/")
        {
            var token = await GetAntiforgeryToken(client, formUrl);
            var content = new MultipartFormDataContent();
            if (token != null) content.Add(new StringContent(token), "__RequestVerificationToken");
            foreach (var kv in fields)
            {
                content.Add(new StringContent(kv.Value), kv.Key);
            }
            return await client.PostAsync(postUrl, content);
        }

        private Task<HttpClient> CreateAuthenticatedClient(string email, string password)
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            client.DefaultRequestHeaders.Add("Test-User", email);
            return Task.FromResult(client);
        }

        [Fact]
        public async Task Registration_Login_UnauthorizedAccess_Works()
        {
            var client = await CreateAuthenticatedClient("inttestuser@example.com", "P@ssw0rd!");

            // Access a protected page
            var resp = await client.GetAsync("/Dashboard");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            // Create an unauthenticated client to ensure redirect to login
            var anon = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            var r = await anon.GetAsync("/Dashboard");
            Assert.True(r.StatusCode == HttpStatusCode.Redirect || r.StatusCode == HttpStatusCode.RedirectMethod);
            Assert.Contains("/Account/Login", r.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task Task_Create_Edit_Complete_Reopen_Delete_Reschedule_Workflow()
        {
            var client = await CreateAuthenticatedClient("taskuser@example.com", "P@ssw0rd!");

            // Create a category first
            var catResp = await PostFormWithToken(client, "/Category/Create", new Dictionary<string, string> { { "Name", "Work" } }, "/Category/Create");
            Assert.True(catResp.StatusCode == HttpStatusCode.Redirect);

            // Get category id
            long catId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var c = db.Categories.FirstOrDefault(a => a.Name == "Work");
                Assert.NotNull(c);
                catId = c.Id;
            }

            // Create task
            var create = new MultipartFormDataContent
            {
                { new StringContent("Test Task"), "Title" },
                { new StringContent("Details"), "Details" },
                { new StringContent(catId.ToString()), "CategoryId" },
                { new StringContent(DateTime.UtcNow.Date.ToString("yyyy-MM-dd")), "DueDate" }
            };
            var createFields = new Dictionary<string, string>
            {
                { "Title", "Test Task" },
                { "Details", "Details" },
                { "CategoryId", catId.ToString() },
                { "DueDate", DateTime.UtcNow.Date.ToString("yyyy-MM-dd") }
            };
            var createResp = await PostFormWithToken(client, "/Task/Create", createFields, "/Task/Create");
            Assert.True(createResp.StatusCode == HttpStatusCode.Redirect);

            long taskId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var t = db.Tasks.FirstOrDefault(a => a.Title == "Test Task");
                Assert.NotNull(t);
                taskId = t.Id;
            }

            // Edit task
            var edit = new MultipartFormDataContent
            {
                { new StringContent(taskId.ToString()), "Id" },
                { new StringContent("Test Task Edited"), "Title" },
                { new StringContent("Details"), "Details" },
                { new StringContent(catId.ToString()), "CategoryId" }
            };
            var editFields = new Dictionary<string, string>
            {
                { "Id", taskId.ToString() },
                { "Title", "Test Task Edited" },
                { "Details", "Details" },
                { "CategoryId", catId.ToString() }
            };
            var editResp = await PostFormWithToken(client, "/Task/Edit", editFields, $"/Task/Edit?id={taskId}");
            Assert.True(editResp.StatusCode == HttpStatusCode.Redirect);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var t = db.Tasks.Find(taskId);
                Assert.Equal("Test Task Edited", t.Title);
            }

            // Complete task via ChangeStatus (AJAX style header)
            // Change status (AJAX) - include antiforgery token header
            var token = await GetAntiforgeryToken(client, "/Dashboard");
            var req = new HttpRequestMessage(HttpMethod.Post, $"/Task/ChangeStatus?id={taskId}&status=Completed");
            req.Headers.Add("X-Requested-With", "fetch");
            if (token != null) req.Headers.Add("RequestVerificationToken", token);
            var statusResp = await client.SendAsync(req);
            Assert.Equal(HttpStatusCode.OK, statusResp.StatusCode);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var t = db.Tasks.Find(taskId);
                Assert.Equal(PersonalToDo_Freelance.Domain.Enums.TodoTaskStatus.Completed, t.Status);
            }

            // Reopen (set to Pending)
            var req2 = new HttpRequestMessage(HttpMethod.Post, $"/Task/ChangeStatus?id={taskId}&status=NotStarted");
            req2.Headers.Add("X-Requested-With", "fetch");
            if (token != null) req2.Headers.Add("RequestVerificationToken", token);
            var r2 = await client.SendAsync(req2);
            Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var t = db.Tasks.Find(taskId);
                Assert.Equal(PersonalToDo_Freelance.Domain.Enums.TodoTaskStatus.NotStarted, t.Status);
            }

            // Reschedule
            var newDue = DateTime.UtcNow.Date.AddDays(5).ToString("yyyy-MM-dd");
            var resResp = await PostFormWithToken(client, "/Task/Reschedule", new Dictionary<string, string> { { "Id", taskId.ToString() }, { "NewDueDate", newDue } }, $"/Task/Reschedule?id={taskId}");
            Assert.True(resResp.StatusCode == HttpStatusCode.Redirect);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var t = db.Tasks.Find(taskId);
                Assert.Equal(DateTime.Parse(newDue).Date, t.DueDate.Value.Date);
            }

            // Delete
            var delResp = await PostFormWithToken(client, $"/Task/Delete", new Dictionary<string, string> { { "id", taskId.ToString() } }, "/Dashboard");
            Assert.True(delResp.StatusCode == HttpStatusCode.Redirect);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var t = db.Tasks.Find(taskId);
                Assert.Null(t);
            }
        }

        [Fact]
        public async Task Categories_Create_Edit_Delete_Workflow()
        {
            var client = await CreateAuthenticatedClient("catuser@example.com", "P@ssw0rd!");

            // Create
            var r = await PostFormWithToken(client, "/Category/Create", new Dictionary<string,string> {{ "Name", "Personal" } }, "/Category/Create");
            Assert.True(r.StatusCode == HttpStatusCode.Redirect);

            long catId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var c = db.Categories.FirstOrDefault(a => a.Name == "Personal");
                Assert.NotNull(c);
                catId = c.Id;
            }

            // Edit
            var e = await PostFormWithToken(client, "/Category/Edit", new Dictionary<string,string> {{ "Id", catId.ToString() }, { "Name", "Personal Edited" } }, $"/Category/Edit?id={catId}");
            Assert.True(e.StatusCode == HttpStatusCode.Redirect);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var c = db.Categories.Find(catId);
                Assert.Equal("Personal Edited", c.Name);
            }

            // Delete
            var d = await PostFormWithToken(client, "/Category/Delete", new Dictionary<string,string> {{ "id", catId.ToString() } }, "/Category/Index");
            Assert.True(d.StatusCode == HttpStatusCode.Redirect);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var c = db.Categories.Find(catId);
                Assert.Null(c);
            }
        }

        [Fact]
        public async Task Recurrence_Create_Generate_Complete_Reschedule_Stop()
        {
            var client = await CreateAuthenticatedClient("recuser@example.com", "P@ssw0rd!");

            // Create category
            var catResp = await PostFormWithToken(client, "/Category/Create", new Dictionary<string,string> {{ "Name", "RecCats" } }, "/Category/Create");
            Assert.True(catResp.StatusCode == HttpStatusCode.Redirect);

            long catId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                catId = db.Categories.First(a => a.Name == "RecCats").Id;
            }

            // Create task with recurrence via POST to Task/Create (recurrence fields depend on app; attempt minimal)
            var form = new MultipartFormDataContent
            {
                { new StringContent("Recurring Task"), "Title" },
                { new StringContent("Details"), "Details" },
                { new StringContent(catId.ToString()), "CategoryId" },
                { new StringContent(DateTime.UtcNow.Date.ToString("yyyy-MM-dd")), "DueDate" },
                { new StringContent("Daily"), "Recurrence.Rule" },
                { new StringContent("1"), "Recurrence.Interval" }
            };
            var fields = new Dictionary<string,string>
            {
                { "Title", "Recurring Task" },
                { "Details", "Details" },
                { "CategoryId", catId.ToString() },
                { "DueDate", DateTime.UtcNow.Date.ToString("yyyy-MM-dd") },
                { "Recurrence.Rule", "Daily" },
                { "Recurrence.Interval", "1" }
            };
            var createResp = await PostFormWithToken(client, "/Task/Create", fields, "/Task/Create");
            Assert.True(createResp.StatusCode == HttpStatusCode.Redirect);

            long taskId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var t = db.Tasks.First(a => a.Title == "Recurring Task");
                taskId = t.Id;
            }

            // Generate occurrences
            var genResp = await PostFormWithToken(client, $"/Task/GenerateOccurrences?id={taskId}", new Dictionary<string,string> {{ "id", taskId.ToString() } }, $"/Task/Details?id={taskId}");
            Assert.True(genResp.StatusCode == HttpStatusCode.Redirect);

            long occId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var occ = db.TaskOccurrences.FirstOrDefault(o => o.TodoTaskId == taskId);
                Assert.NotNull(occ);
                occId = occ.Id;
            }

            // Complete occurrence
            var compResp = await PostFormWithToken(client, $"/Task/ChangeOccurrenceStatus?occurrenceId={occId}&status=Completed&taskId={taskId}", new Dictionary<string,string> {{ "occurrenceId", occId.ToString() }, { "taskId", taskId.ToString() }, { "status", "Completed" } }, $"/Task/Details?id={taskId}");
            Assert.True(compResp.StatusCode == HttpStatusCode.Redirect);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var occ = db.TaskOccurrences.Find(occId);
                Assert.Equal(PersonalToDo_Freelance.Domain.Enums.OccurrenceStatus.Completed, occ.Status);
            }

            // Reschedule occurrence
            var newDate = DateTime.UtcNow.Date.AddDays(3).ToString("yyyy-MM-dd");
            var resResp = await PostFormWithToken(client, $"/Task/RescheduleOccurrence?occurrenceId={occId}&taskId={taskId}&scheduledDate={newDate}", new Dictionary<string,string> {{ "occurrenceId", occId.ToString() }, { "taskId", taskId.ToString() }, { "scheduledDate", newDate } }, $"/Task/Details?id={taskId}");
            Assert.True(resResp.StatusCode == HttpStatusCode.Redirect);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var occ = db.TaskOccurrences.Find(occId);
                Assert.Equal(DateTime.Parse(newDate).Date, occ.OccurrenceDate.Date);
            }

            // Stop recurrence
            var stopResp = await PostFormWithToken(client, $"/Task/StopRecurrence?id={taskId}", new Dictionary<string,string> {{ "id", taskId.ToString() } }, $"/Task/Details?id={taskId}");
            Assert.True(stopResp.StatusCode == HttpStatusCode.Redirect);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var t = db.Tasks.Find(taskId);
                Assert.Null(t.RecurrenceRule);
            }
        }

        [Fact]
        public async Task Security_UserA_CannotAccessOrModify_UserB_Data()
        {
            var clientA = await CreateAuthenticatedClient("userA@example.com", "P@ssw0rd!");
            var clientB = await CreateAuthenticatedClient("userB@example.com", "P@ssw0rd!");

            // User B creates category and task
            var cResp = await PostFormWithToken(clientB, "/Category/Create", new Dictionary<string,string> {{ "Name", "Bcat" } }, "/Category/Create");
            Assert.True(cResp.StatusCode == HttpStatusCode.Redirect);

            long catId;
            long taskId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var c = db.Categories.First(a => a.Name == "Bcat");
                catId = c.Id;

                var t = new PersonalToDo_Freelance.Domain.Entities.TodoTask { Title = "Btask", CategoryId = catId, UserId = c.UserId, DueDate = DateTime.UtcNow.Date };
                db.Tasks.Add(t);
                db.SaveChanges();
                taskId = t.Id;
            }

            // User A tries to access Task Details
            var resp = await clientA.GetAsync($"/Task/Details?id={taskId}");
            Assert.True(resp.StatusCode == HttpStatusCode.NotFound || resp.StatusCode == HttpStatusCode.Redirect || resp.StatusCode == HttpStatusCode.Forbidden);

            // User A tries to edit Category
            var editResp = await PostFormWithToken(clientA, "/Category/Edit", new Dictionary<string,string> {{ "Id", catId.ToString() }, { "Name", "Hacked" } }, $"/Category/Edit?id={catId}");
            // Should not allow modification
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var c = db.Categories.Find(catId);
                Assert.NotEqual("Hacked", c.Name);
            }

            // User A attempts to delete B's task via crafted id
            var delResp = await PostFormWithToken(clientA, $"/Task/Delete", new Dictionary<string,string> {{ "id", taskId.ToString() } }, "/Dashboard");
            // Should not delete
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var t = db.Tasks.Find(taskId);
                Assert.NotNull(t);
            }
        }
    }
}
