using UnityEngine;
using UnityEngine.UI; // Buttonコンポーネントを操作する場合

public class ClickEffectController : MonoBehaviour
{
    // ★ 発生させるエフェクトのプレハブをInspectorからアタッチします
    public GameObject effectPrefab;

    // エフェクトを再生するメソッド
    public void PlayEffect()
    {
        if (effectPrefab == null)
        {
            Debug.LogError("エフェクトのプレハブが設定されていません。");
            return;
        }

        Vector3 effectPosition = transform.position;

        // 1. エフェクトをボタンの位置にインスタンス化（生成）します
        // Quaternion.identity は回転なしを意味します
        GameObject effect = Instantiate(effectPrefab, transform.position, Quaternion.identity);

        // ボタンを親(Parent)に設定
        effect.transform.SetParent(transform, true); // transform.positionを維持しつつ親を設定

        // 2. インスタンス化されたエフェクトを再生します
        ParticleSystem ps = effect.GetComponent<ParticleSystem>();

        if (ps != null)
        {
            effect.SetActive(true);

            ps.Play();

            // 3. エフェクトの再生が終了したら自動的に削除されるように設定します
            // 寿命が尽きたら自動削除される仕組みをParticleSystem側で設定していない場合、このコードが必要です。
            // (例: Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);)

            // ここではシンプルに、エフェクトの持続時間後にオブジェクトを破棄します
            Destroy(effect, ps.main.duration + 0.1f);
        }
        else
        {
            // ParticleSystemコンポーネントがない場合のエラー処理
            Destroy(effect);
        }
    }
}