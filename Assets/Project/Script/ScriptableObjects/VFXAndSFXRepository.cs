using UnityEngine;

namespace Gazeus.DesafioMatch3.ScriptableObjects
{
    [CreateAssetMenu(fileName = "VFXAndSFXRepository", menuName = "Gameplay/VFXAndSFXRepository")]
    public class VFXAndSFXRepository : ScriptableObject
    {
        [SerializeField] private GameObject _bombParticlePrefab;
        [SerializeField] private AudioClip _bombSound;

        public GameObject BombParticlePrefab => _bombParticlePrefab;
        public AudioClip BombSound => _bombSound;
    }
}