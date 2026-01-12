using System.Threading.Tasks;

namespace SapCrossfadeAudio.Addressables
{
    /// <summary>
    /// 事前ロードが可能な AudioGenerator のインターフェース。
    /// Addressables や外部リソースを使用する Generator が実装する。
    /// </summary>
    public interface IPreloadableAudioGenerator
    {
        /// <summary>
        /// アセットがロード済みで、再生可能な状態かどうか。
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// アセットを事前にロードする。
        /// 呼び出し元は完了を待ってから再生を開始することで、ヒッチを回避できる。
        /// </summary>
        /// <returns>ロード完了を待つタスク</returns>
        Task PreloadAsync();

        /// <summary>
        /// ロードしたアセットを解放する。
        /// このメソッドは冪等（何度呼んでも安全）でなければならない。
        /// </summary>
        void Release();
    }
}
