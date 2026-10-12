using System;
using System.Collections.Generic;
using CadCoreFramework.Core;
using CadCoreFramework.Models;
using CadCoreFramework.Services;
using CadCoreFramework.ViewModels;
using FluentAssertions;
using Moq;
using Xunit;

namespace CadCoreFramework.Tests
{
    public class LayerManagerViewModelTests
    {
        private readonly Mock<ILayerService> _service = new Mock<ILayerService>();
        private readonly Mock<INotificationService> _notify = new Mock<INotificationService>();

        private LayerManagerViewModel NewViewModel() => new LayerManagerViewModel(_service.Object, _notify.Object);

        [Fact]
        public void LoadLayers_FillsCollection()
        {
            _service.Setup(s => s.GetAllLayers()).Returns(new List<LayerModel>
            {
                new LayerModel("0", true, 7),
                new LayerModel("E_LIGHT", false, 2)
            });
            var vm = NewViewModel();

            vm.LoadLayersCommand.Execute(null);

            vm.Layers.Should().HaveCount(2);
            vm.IsLoading.Should().BeFalse();
        }

        [Fact]
        public void LoadLayers_ServiceThrows_ReportsError()
        {
            _service.Setup(s => s.GetAllLayers()).Throws(new InvalidOperationException("no doc"));
            var vm = NewViewModel();

            vm.LoadLayersCommand.Execute(null);

            _notify.Verify(n => n.Error(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
            vm.IsLoading.Should().BeFalse();
        }

        [Fact]
        public void ToggleLayer_Success_FlipsState()
        {
            var layer = new LayerModel("E", isOn: true, colorIndex: 1);
            _service.Setup(s => s.ToggleLayerVisibility("E", false)).Returns(CadResult<bool>.Success(true));

            NewViewModel().ToggleLayerCommand.Execute(layer);

            layer.IsOn.Should().BeFalse();
        }

        [Fact]
        public void ToggleLayer_LayerMissing_KeepsStateAndWarns()
        {
            var layer = new LayerModel("GONE", isOn: true, colorIndex: 1);
            _service.Setup(s => s.ToggleLayerVisibility("GONE", false)).Returns(CadResult<bool>.Success(false));

            NewViewModel().ToggleLayerCommand.Execute(layer);

            layer.IsOn.Should().BeTrue();
            _notify.Verify(n => n.Warn(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void ToggleLayer_CadFailure_KeepsStateAndReportsError()
        {
            var layer = new LayerModel("E", isOn: true, colorIndex: 1);
            _service.Setup(s => s.ToggleLayerVisibility("E", false)).Returns(CadResult<bool>.Fail("locked"));

            NewViewModel().ToggleLayerCommand.Execute(layer);

            layer.IsOn.Should().BeTrue();
            _notify.Verify(n => n.Error(It.Is<string>(m => m.Contains("locked")), It.IsAny<Exception>()), Times.Once);
        }
    }
}
