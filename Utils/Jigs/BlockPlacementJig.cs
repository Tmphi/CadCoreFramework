using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace CadCoreFramework.Utils.Jigs
{
    /// <summary>
    /// Lớp hỗ trợ tạo hiệu ứng kéo lê (Jig) đối tượng Block trên màn hình trước khi click chuột đặt xuống.
    /// Cách dùng:
    /// var jig = new BlockPlacementJig(blockRef);
    /// var result = Editor.Drag(jig);
    /// if (result.Status == PromptStatus.OK) { ... }
    /// </summary>
    public class BlockPlacementJig : EntityJig
    {
        private Point3d _position;
        private readonly BlockReference _blockRef;
        private readonly string _promptMessage;

        /// <summary>
        /// Khởi tạo một phiên bản BlockPlacementJig với đối tượng BlockReference và câu nhắc tương ứng.
        /// </summary>
        public BlockPlacementJig(BlockReference blockRef, string promptMessage = "\nChọn điểm đặt: ") 
            : base(blockRef)
        {
            _blockRef = blockRef;
            _position = blockRef.Position;
            _promptMessage = promptMessage;
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            var options = new JigPromptPointOptions(_promptMessage)
            {
                UserInputControls = UserInputControls.Accept3dCoordinates | 
                                    UserInputControls.NoNegativeResponseAccepted | 
                                    UserInputControls.NullResponseAccepted
            };

            var result = prompts.AcquirePoint(options);

            if (result.Value.IsEqualTo(_position))
                return SamplerStatus.NoChange;

            _position = result.Value;
            return SamplerStatus.OK;
        }

        protected override bool Update()
        {
            try
            {
                _blockRef.Position = _position;
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Vị trí tọa độ điểm đặt hiện tại của Block.
        /// </summary>
        public Point3d Position => _position;
    }
}
