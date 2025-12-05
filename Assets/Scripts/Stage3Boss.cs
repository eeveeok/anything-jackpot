using System.Collections;
using UnityEngine;

public class Stage3Boss : MonoBehaviour
{
    public enum BossPattern
    {
        Idle,
        Follow,         // 기본 추적 AI
        Slam,           // 손바닥 내려찍기
        EnergyWave,     // 바닥 파동
        Rush,           // 연속 돌진
        Laser,          // 전면 레이저
        Rage            // 분노 모드 돌입
    }

    [Header("레퍼런스")]
    public Transform player;
    public Animator anim;
    public LaserBeam laserBeam;
    public GameObject energyWavePrefab;

    [Header("보스 스탯")]
    public float maxHP = 1000f;
    public float currentHP;

    [Header("추적 AI")]
    public float followSpeed = 10f;
    public float followStopDistance = 0.5f;   // 플레이어와 너무 가까우면 이동 멈춤

    [Header("패턴 설정")]
    public float idleDelay = 3f;
    public int slamCount = 3;
    public float slamInterval = 0.8f;
    public float waveInterval = 0.3f;
    public float waveSpacing = 1.5f;
    public int rushTimes = 4;
    public float rushSpeed = 12f;

    private Rigidbody2D rb;
    private bool isRage = false;
    private bool isInPattern = false; // 패턴 중에는 추적 중지

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        currentHP = maxHP;
        StartCoroutine(BossRoutine());
    }

    void Update()
    {
        if (!isInPattern)
        {
            FollowPlayer();
        }
    }

    // ----------------------------------------------------------
    //              기본 추적 AI
    // ----------------------------------------------------------
    void FollowPlayer()
    {
        if (player == null || player.GetComponent<LaserShooter>().isDead) return;

        float distance = Vector2.Distance(transform.position, player.position);

        // 너무 가까우면 멈춤
        if (distance <= followStopDistance)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        // 플레이어 방향
        Vector2 dir = (player.position - transform.position).normalized;
        dir.y = 0f;
        dir.Normalize(); // y를 0으로 만들었으니 다시 정규화

        rb.velocity = dir * followSpeed;

        // 애니메이션 이동 모션
        //anim.SetBool("moving", true);
    }


    // ----------------------------------------------------------
    //              보스 패턴 메인 루프
    // ----------------------------------------------------------
    IEnumerator BossRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(idleDelay);

            // HP 50% 이하 → 분노 돌입
            if (!isRage && currentHP <= maxHP * 0.5f)
            {
                isRage = true;
                //anim.SetTrigger("rage");
                Debug.Log("보스 분노 모드 돌입!");
                yield return new WaitForSeconds(1f);
            }

            // 패턴 선택
            if (!isRage)
            {
                int p = Random.Range(0, 2);

                if (p == 0)
                    yield return StartCoroutine(Pattern_Slam());
                else
                    yield return StartCoroutine(Pattern_EnergyWave());
            }
            else
            {
                int p = Random.Range(0, 3);

                if (p == 0)
                    yield return StartCoroutine(Pattern_Rush());
                else if (p == 1)
                    yield return StartCoroutine(Pattern_Laser());
                else
                    yield return StartCoroutine(Pattern_Slam());
            }
        }
    }

    // ----------------------------------------------------------
    //              개별 패턴 정의
    // ----------------------------------------------------------

    IEnumerator Pattern_Slam()
    {
        isInPattern = true;

        rb.velocity = Vector2.zero;
        //anim.SetBool("moving", false);

        Debug.Log("패턴: 손바닥 내려찍기 (360도 범위)");

        float slamRadius = 3f; // 공격 범위 반지름

        for (int i = 0; i < slamCount; i++)
        {
            // 손바닥 내려찍기 애니메이션 재생
            //anim.SetTrigger("slam");

            // 잠시 대기 후 피해 처리 (애니메이션 타이밍 맞추기)
            yield return new WaitForSeconds(slamInterval * 0.5f);

            // 360도 범위 공격
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, slamRadius, LayerMask.GetMask("Player"));
            foreach (Collider2D hit in hits)
            {
                LaserShooter player = hit.GetComponent<LaserShooter>();
                if (player != null)
                {
                    player.PlayerDie(); // 플레이어 즉시 죽이기
                }
            }

            yield return new WaitForSeconds(slamInterval * 0.5f);
        }

        isInPattern = false;
    }

    // 시각화용 Gizmos
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        float slamRadius = 3f;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, slamRadius);
    }



    IEnumerator Pattern_EnergyWave()
    {
        isInPattern = true;

        rb.velocity = Vector2.zero;

        Debug.Log("패턴: 에너지 웨이브");

        // 바닥 y 위치 고정
        float groundY = transform.position.y - 0.7f;

        // 파동 설정
        int maxWaves = 10;         // 플레이어까지가 아니라 최대 몇 개 생성할지

        // 좌우 방향
        int direction = (player.position.x > transform.position.x) ? 1 : -1; // 1 = 오른쪽, -1 = 왼쪽

        for (int i = 1; i <= maxWaves; i++)
        {
            Vector2 spawnPos = new Vector2(transform.position.x + i * waveSpacing * direction, groundY);

            GameObject wave = Instantiate(energyWavePrefab, spawnPos, Quaternion.identity);
            Destroy(wave, 1f);

            yield return new WaitForSeconds(waveInterval);
        }

        yield return new WaitForSeconds(0.5f);

        isInPattern = false;
    }

    IEnumerator Pattern_Rush()
    {
        isInPattern = true;

        rb.velocity = Vector2.zero;
        //anim.SetBool("moving", false);

        Debug.Log("패턴: 연속 돌진");

        for (int i = 0; i < rushTimes; i++)
        {
            //anim.SetTrigger("rush");

            Vector2 dir = (player.position - transform.position).normalized;
            float timer = 0;

            while (timer < 0.45f)
            {
                timer += Time.deltaTime;
                rb.velocity = dir * rushSpeed;
                yield return null;
            }

            rb.velocity = Vector2.zero;
            yield return new WaitForSeconds(0.4f);
        }

        isInPattern = false;
    }

    IEnumerator Pattern_Laser()
    {
        isInPattern = true;

        rb.velocity = Vector2.zero;
        //anim.SetBool("moving", false);

        Debug.Log("패턴: 전면 레이저");

        //anim.SetTrigger("laserStart");
        yield return new WaitForSeconds(0.8f);

        laserBeam.SetActive(true);

        yield return new WaitForSeconds(3f);

        laserBeam.SetActive(false);

        isInPattern = false;
    }

    // ----------------------------------------------------------
    //                 보스 데미지 처리
    // ----------------------------------------------------------
    public void ApplyDamage(float damage)
    {
        currentHP -= damage;

        if (currentHP <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        StopAllCoroutines();
        rb.velocity = Vector2.zero;

        isInPattern = true; // 죽었으니 모든 AI 중지

        //anim.SetTrigger("die");

        Debug.Log("보스 사망!");

        Destroy(gameObject, 3f);
    }
}