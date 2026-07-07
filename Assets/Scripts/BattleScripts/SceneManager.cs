using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SNJ.Scene
{
    public class SceneManager : MonoBehaviour
    {
        public static SceneManager Instance { get; private set; }

        /// <summary>
        /// 게임 시작 시 자동으로 씬을 이동할지 여부
        /// </summary>
        [Header("Auto Load Setting")]
        [SerializeField] bool m_autoLoadOnStart = true;

        /// <summary>
        /// 자동으로 이동할 씬 이름
        /// </summary>
        [SerializeField] string m_autoLoadSceneName = "GameScene";

        /// <summary>
        /// 자동 이동 전 대기 시간
        /// </summary>
        [SerializeField] float m_autoLoadDelay = 0.5f;

        /// <summary>
        /// fade 이미지
        /// </summary>
        [Header("Fade Setting")]
        [SerializeField] Image m_fadeImg = null;

        /// <summary>
        /// Fade 컬러
        /// 0 : in , 1 : out
        /// </summary>
        [SerializeField] Color[] m_fadeColorArr = null;

        /// <summary>
        /// 로딩 오브젝트 배열
        /// 0 : 텍스트 , 1 : 로딩 슬라이더
        /// </summary>
        [SerializeField] GameObject[] m_loadingObjArr = null;

        /// <summary>
        /// 로딩 슬라이더
        /// </summary>
        [SerializeField] Slider m_loadingSlider = null;

        /// <summary>
        /// 현재의 컬러
        /// </summary>
        Color m_nowColor = Color.black;

        /// <summary>
        /// 씬 이동 플래그
        /// </summary>
        bool m_changeSceneFlag = false;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (m_fadeImg != null)
            {
                m_fadeImg.raycastTarget = false;
            }

            if (m_loadingObjArr != null)
            {
                for (int i = 0; i < m_loadingObjArr.Length; i++)
                {
                    if (m_loadingObjArr[i] != null)
                    {
                        m_loadingObjArr[i].SetActive(false);
                    }
                }
            }
        }

        void Start()
        {
            if (m_autoLoadOnStart)
            {
                StartCoroutine(AutoLoadStart());
            }
        }

        IEnumerator AutoLoadStart()
        {
            yield return new WaitForSeconds(m_autoLoadDelay);

            if (!string.IsNullOrEmpty(m_autoLoadSceneName))
            {
                Load(m_autoLoadSceneName);
            }
            else
            {
                Debug.LogWarning("자동 이동할 씬 이름이 비어 있습니다.");
            }
        }

        /// <summary>
        /// 씬 로드
        /// </summary>
        /// <param name="argSceneName">이동할 씬 이름</param>
        public void Load(string argSceneName)
        {
            if (m_changeSceneFlag) return;

            m_changeSceneFlag = true;

            StartCoroutine(ChangeScene(argSceneName));
        }

        /// <summary>
        /// 씬 이동
        /// </summary>
        /// <param name="argSceneName">이동할 씬 이름</param>
        /// <returns></returns>
        IEnumerator ChangeScene(string argSceneName)
        {
            if (m_fadeImg == null)
            {
                Debug.LogError("Fade Image가 연결되지 않았습니다.");
                m_changeSceneFlag = false;
                yield break;
            }

            if (m_loadingSlider == null)
            {
                Debug.LogError("Loading Slider가 연결되지 않았습니다.");
                m_changeSceneFlag = false;
                yield break;
            }

            if (m_fadeColorArr == null || m_fadeColorArr.Length < 2)
            {
                Debug.LogError("Fade Color Arr는 최소 2개가 필요합니다. 0번은 투명, 1번은 불투명 색상입니다.");
                m_changeSceneFlag = false;
                yield break;
            }

            m_fadeImg.raycastTarget = true;

            if (m_loadingObjArr != null)
            {
                for (int i = 0; i < m_loadingObjArr.Length; i++)
                {
                    if (m_loadingObjArr[i] != null)
                    {
                        m_loadingObjArr[i].SetActive(true);
                    }
                }
            }

            m_loadingSlider.value = 0.0f;

            m_nowColor = m_fadeColorArr[0];
            m_nowColor.a = m_fadeColorArr[0].a;
            m_fadeImg.color = m_nowColor;

            while (m_nowColor.a != m_fadeColorArr[1].a)
            {
                m_nowColor.a += Time.deltaTime;
                m_nowColor.a = m_nowColor.a > m_fadeColorArr[1].a ? m_fadeColorArr[1].a : m_nowColor.a;
                m_fadeImg.color = m_nowColor;
                yield return null;
            }

            AsyncOperation _async = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(argSceneName);

            if (_async == null)
            {
                Debug.LogError("씬을 찾을 수 없습니다: " + argSceneName);
                m_changeSceneFlag = false;
                yield break;
            }

            _async.allowSceneActivation = false;

            while (_async.progress < 0.9f)
            {
                m_loadingSlider.value = _async.progress / 0.9f;
                yield return null;
            }

            m_loadingSlider.value = 1.0f;

            yield return new WaitForSeconds(0.2f);

            _async.allowSceneActivation = true;

            yield return null;

            m_nowColor = m_fadeColorArr[1];
            m_nowColor.a = m_fadeColorArr[1].a;
            m_fadeImg.color = m_nowColor;

            while (m_nowColor.a != m_fadeColorArr[0].a)
            {
                m_nowColor.a -= Time.deltaTime;
                m_nowColor.a = m_nowColor.a < m_fadeColorArr[0].a ? m_fadeColorArr[0].a : m_nowColor.a;
                m_fadeImg.color = m_nowColor;
                yield return null;
            }

            if (m_loadingObjArr != null)
            {
                for (int i = 0; i < m_loadingObjArr.Length; i++)
                {
                    if (m_loadingObjArr[i] != null)
                    {
                        m_loadingObjArr[i].SetActive(false);
                    }
                }
            }

            m_fadeImg.raycastTarget = false;

            m_changeSceneFlag = false;
        }

        public bool IsLoad
        {
            get { return m_changeSceneFlag; }
        }
    }
}