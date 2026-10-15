using UnityEngine;

namespace TorreDeControle.UI
{
    /// <summary>Canal de efeitos da interface, independente da música. Sem estado estático.</summary>
    public sealed class SonsUI : MonoBehaviour
    {
        AudioSource fonte;
        AudioClip cursor;
        AudioClip confirmar;
        AudioClip cancelar;
        float ultimoCursor = -1;

        public void Configurar(AudioClip somCursor, AudioClip somConfirmar, AudioClip somCancelar)
        {
            cursor = somCursor;
            confirmar = somConfirmar;
            cancelar = somCancelar;
            fonte = gameObject.AddComponent<AudioSource>();
            fonte.playOnAwake = false;
            fonte.loop = false;
            fonte.spatialBlend = 0;
            fonte.volume = 0.7f;
        }

        public void Cursor()
        {
            // O clique também pode selecionar o botão: evita dois sons no mesmo instante.
            if (Time.unscaledTime - ultimoCursor < 0.06f) return;
            ultimoCursor = Time.unscaledTime;
            Tocar(cursor);
        }

        public void Acionar(bool cancelamento) => Tocar(cancelamento ? cancelar : confirmar);

        void Tocar(AudioClip clip)
        {
            if (fonte != null && clip != null) fonte.PlayOneShot(clip);
        }
    }
}
