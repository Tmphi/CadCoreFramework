using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CadCoreFramework.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace CadCoreFramework.ViewModels
{
    /// <summary>
    /// ViewModel cho Debug Inspector Palette. Cung cấp view live vào ba hệ
    /// thống nền của framework:
    ///   1. Command History  — lệnh gần đây + duration + success/fail
    ///   2. DI Services      — service nào đã đăng ký (lifetime, interface, impl)
    ///   3. Event Subscribers — số handler đang nghe mỗi message type
    ///
    /// Palette bind trực tiếp vào ObservableCollection; nhấn RefreshCommand
    /// để pull data mới nhất từ các service.
    /// </summary>
    public partial class DebugInspectorViewModel : ViewModelBase
    {
        private readonly ICommandHistoryService? _history;
        private readonly IServiceCollection? _registeredServices;

        public ObservableCollection<CommandHistoryRow> CommandHistory { get; } = new ObservableCollection<CommandHistoryRow>();
        public ObservableCollection<ServiceRow> Services { get; } = new ObservableCollection<ServiceRow>();
        public ObservableCollection<string> EventSubscribers { get; } = new ObservableCollection<string>();

        [ObservableProperty]
        private string _summary = string.Empty;

        public DebugInspectorViewModel(ICommandHistoryService history)
        {
            _history = history;
            // AppBootstrapper exposes the IServiceCollection used to build the provider
            // via a static accessor (added separately — see AppBootstrapper.RegisteredServices).
            _registeredServices = AppBootstrapper.RegisteredServices;
            Title = "Debug Inspector";
            Refresh();
        }

        [RelayCommand]
        private void Refresh()
        {
            // ─── Command history ─────────────────────────────────────────────
            CommandHistory.Clear();
            if (_history != null)
            {
                foreach (var rec in _history.RecentCommands)
                {
                    CommandHistory.Add(new CommandHistoryRow(
                        rec.CommandName,
                        rec.Duration.TotalMilliseconds,
                        rec.Success,
                        rec.StartedAtUtc.ToLocalTime().ToString("HH:mm:ss")));
                }
            }

            // ─── DI Services ─────────────────────────────────────────────────
            Services.Clear();
            if (_registeredServices != null)
            {
                foreach (var sd in _registeredServices.OrderBy(s => s.ServiceType.Name))
                {
                    Services.Add(new ServiceRow(
                        sd.ServiceType.Name,
                        sd.ImplementationType?.Name ?? "<factory>",
                        sd.Lifetime.ToString()));
                }
            }

            // ─── Event Subscribers ───────────────────────────────────────────
            EventSubscribers.Clear();
            var aggregator = AppBootstrapper.ServiceProvider?.GetService<IEventAggregator>() as EventAggregator;
            if (aggregator != null)
            {
                foreach (var line in aggregator.GetSubscriberSnapshot())
                {
                    EventSubscribers.Add(line);
                }
            }

            Summary = $"{CommandHistory.Count} commands · {Services.Count} services · {EventSubscribers.Count} subscriptions";
        }
    }

    /// <summary>Row DTO for the Command History DataGrid.</summary>
    public sealed class CommandHistoryRow
    {
        public string Name { get; }
        public double DurationMs { get; }
        public bool Success { get; }
        public string StartedAt { get; }

        public CommandHistoryRow(string name, double durationMs, bool success, string startedAt)
        {
            Name = name;
            DurationMs = durationMs;
            Success = success;
            StartedAt = startedAt;
        }
    }

    /// <summary>Row DTO for the DI Services DataGrid.</summary>
    public sealed class ServiceRow
    {
        public string ServiceType { get; }
        public string ImplementationType { get; }
        public string Lifetime { get; }

        public ServiceRow(string serviceType, string implementationType, string lifetime)
        {
            ServiceType = serviceType;
            ImplementationType = implementationType;
            Lifetime = lifetime;
        }
    }
}
