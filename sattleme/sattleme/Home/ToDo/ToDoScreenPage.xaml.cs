using sattleme.Home.Rental;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace sattleme.Home.ToDo
{
    public partial class ToDoScreenPage : ContentPage
    {
        public string UserId { get; set; }
        public ObservableCollection<DateItem> DateItems { get; set; } = new ObservableCollection<DateItem>();
        public List<TaskCategory> TaskCategories { get; set; } = new List<TaskCategory>();
        public int TodayIndex { get; set; }
        public bool AllTasksCompleted { get; set; } = false;

        public ToDoScreenPage(string userId)
        {
            InitializeComponent();
            UserId = userId;
            BindingContext = this;
            LoadDateItems();
            LoadTasks();
        }

        void LoadDateItems()
        {
            DateTime now = DateTime.Now;
            TodayIndex = now.DayOfWeek == DayOfWeek.Sunday ? 6 : ((int)now.DayOfWeek - 1);
            DateItems.Clear();
            DateTime monday = now.AddDays(-(int)now.DayOfWeek + 1);
            for (int i = 0; i < 7; i++)
            {
                DateTime day = monday.AddDays(i);
                DateItems.Add(new DateItem
                {
                    Day = day.ToString("ddd").ToUpper(),
                    Date = day.Day.ToString(),
                    IsSelected = (i == TodayIndex)
                });
            }
        }

        async void LoadTasks()
        {
            TaskCategories = new List<TaskCategory>
            {
                new TaskCategory("First Step", new List<TaskItem>
                {
                    new TaskItem("Book A Room", isUnlocked: true),
                    new TaskItem("Find Ride")
                }),
                new TaskCategory("PERSONAL", new List<TaskItem>
                {
                    new TaskItem("Made SIN"),
                    new TaskItem("Open Bank Account")
                }),
                new TaskCategory("Others", new List<TaskItem>
                {
                    new TaskItem("Transit Information"),
                    new TaskItem("Library")
                }),
                new TaskCategory("TAX FILING", new List<TaskItem>
                {
                    new TaskItem("Tax filing Done or Book a professional Tax filer", isUnlocked: ShouldUnlockTaxFiling())
                })
            };

            BuildTasksUI();
            await LoadSavedTasks();
        }

        bool ShouldUnlockTaxFiling()
        {
            DateTime unlockDate = new DateTime(2025, 2, 28);
            bool allPrevCompleted = TaskCategories.Count >= 3 &&
                TaskCategories.GetRange(0, 3).TrueForAll(cat => cat.Tasks.TrueForAll(t => t.IsCompleted));
            return DateTime.Now > unlockDate && allPrevCompleted;
        }

        void BuildTasksUI()
        {
            TasksStackLayout.Children.Clear();
            foreach (var category in TaskCategories)
            {
                TasksStackLayout.Children.Add(new Label
                {
                    Text = category.Name.ToUpper(),
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.Brown,
                    Margin = new Thickness(0, 8, 0, 4)
                });

                foreach (var task in category.Tasks)
                {
                    var frame = new Frame
                    {
                        CornerRadius = 12,
                        Padding = 12,
                        Margin = new Thickness(0, 4),
                        BackgroundColor = Color.FromHex("#EEEEEE"),
                        HasShadow = false
                    };

                    
                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var checkBox = new CheckBox
                    {
                        IsEnabled = task.IsUnlocked,
                        IsChecked = task.IsCompleted,
                        VerticalOptions = LayoutOptions.Center
                    };
                    checkBox.CheckedChanged += async (s, e) =>
                    {
                        task.IsCompleted = e.Value;
                        await OnTaskChecked();
                    };

                    grid.Children.Add(checkBox, 0, 0);
                    grid.Children.Add(new Label
                    {
                        Text = task.Title,
                        FontSize = 18,
                        TextColor = task.IsUnlocked ? Color.Black : Color.Gray,
                        VerticalOptions = LayoutOptions.Center
                    }, 1, 0);

                    var actionButton = new Button
                    {
                        Text = "Action",
                        BackgroundColor = Color.Blue,
                        TextColor = Color.White,
                        CornerRadius = 5,
                        FontSize = 14,
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.End
                    };
                    actionButton.Clicked += async (s, e) =>
                    {
                       await Navigation.PushAsync(new AllListingsPage());
                    };

                    grid.Children.Add(actionButton, 2, 0);

                    frame.Content = grid;
                    TasksStackLayout.Children.Add(frame);
                }
            }
        }

        async System.Threading.Tasks.Task OnTaskChecked()
        {
            foreach (var category in TaskCategories)
            {
                for (int i = 0; i < category.Tasks.Count - 1; i++)
                {
                    if (category.Tasks[i].IsCompleted)
                        category.Tasks[i + 1].IsUnlocked = true;
                }
            }
            for (int c = 0; c < TaskCategories.Count - 1; c++)
            {
                if (TaskCategories[c].Tasks.TrueForAll(t => t.IsCompleted))
                {
                    foreach (var task in TaskCategories[c + 1].Tasks)
                    {
                        task.IsUnlocked = true;
                    }
                }
            }

            AllTasksCompleted = TaskCategories.TrueForAll(cat => cat.Tasks.TrueForAll(t => t.IsCompleted));
            await SaveTaskCompletion();
            BuildTasksUI();
        }

        async System.Threading.Tasks.Task SaveTaskCompletion()
        {
            foreach (var category in TaskCategories)
            {
                foreach (var task in category.Tasks)
                {
                    Preferences.Set($"{UserId}_{task.Title}_completed", task.IsCompleted);
                    Preferences.Set($"{UserId}_{task.Title}_unlocked", task.IsUnlocked);
                }
            }
        }

        async System.Threading.Tasks.Task LoadSavedTasks()
        {
            foreach (var category in TaskCategories)
            {
                foreach (var task in category.Tasks)
                {
                    task.IsCompleted = Preferences.Get($"{UserId}_{task.Title}_completed", false);
                    task.IsUnlocked = Preferences.Get($"{UserId}_{task.Title}_unlocked", task.IsUnlocked);
                }
            }
            BuildTasksUI();
        }
    }

    public class TaskItem
    {
        public string Title { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsUnlocked { get; set; }
        public TaskItem(string title, bool isUnlocked = false)
        {
            Title = title;
            IsCompleted = false;
            IsUnlocked = isUnlocked;
        }
    }

    public class TaskCategory
    {
        public string Name { get; set; }
        public List<TaskItem> Tasks { get; set; }
        public TaskCategory(string name, List<TaskItem> tasks)
        {
            Name = name;
            Tasks = tasks;
        }
    }

    public class DateItem
    {
        public string Day { get; set; }
        public string Date { get; set; }
        public bool IsSelected { get; set; }
    }
}

