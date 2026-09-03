/// <summary>
/// รายชื่อ id ของเสียงทั้งหมด — ใช้ค่าคงที่แทนการพิมพ์สตริงเอง
/// พิมพ์ผิดจะ compile error ทันที แทนที่จะเงียบ ๆ ไม่มีเสียง
///
/// เอา id พวกนี้ไปกรอกใน list Sounds ของ AudioManager ให้ตรงกัน
/// </summary>
public static class AudioIds
{
    // ── Music ──
    public const string MusicMenu           = "music_menu";
    public const string MusicGameplay       = "music_gameplay";
    public const string MusicBossFirewall   = "music_boss_firewall";
    public const string MusicBossRam        = "music_boss_ram";
    public const string MusicBossRegistry   = "music_boss_registry";
    public const string MusicVictory        = "music_victory";

    // ── Player ──
    public const string PlayerShoot         = "player_shoot";
    public const string PlayerDash          = "player_dash";
    public const string PlayerHit           = "player_hit";
    public const string PlayerDeath         = "player_death";
    public const string PlayerReborn        = "player_reborn";

    // ── Combat ──
    public const string BulletHitEnemy      = "bullet_hit_enemy";
    public const string BulletHitWall       = "bullet_hit_wall";
    public const string EnemyDeath          = "enemy_death";
    public const string EnemySpawn          = "enemy_spawn";

    // ── Enemy ──
    public const string ChaserLunge         = "chaser_lunge";
    public const string SwarmerSplit        = "swarmer_split";
    public const string TurretCharge        = "turret_charge";

    // ── Boss ──
    public const string BossWarning         = "boss_warning";
    public const string BossAppear          = "boss_appear";
    public const string BossDeath           = "boss_death";
    public const string BossShootFirewall   = "boss_shoot_firewall";
    public const string BossShootRam        = "boss_shoot_ram";
    public const string BossShootRegistry   = "boss_shoot_registry";
    public const string LaserCharge         = "laser_charge";

    // ── Flow ──
    public const string WaveStart           = "wave_start";
    public const string WaveCleared         = "wave_cleared";
    public const string SectorExit          = "sector_exit";
    public const string ForceFormat         = "force_format";

    // ── Fragment ──
    public const string FragmentDrop        = "fragment_drop";
    public const string FragmentPickup      = "fragment_pickup";
    public const string FragmentExpire      = "fragment_expire";
    public const string ItemPickup          = "item_pickup";

    // ── Warning / Ambience ──
    public const string LowHealthBeat       = "low_health_beat";
    public const string GlitchAmbience      = "glitch_ambience";

    // ── UI ──
    public const string UIClick             = "ui_click";
    public const string UIPurchase          = "ui_purchase";
    public const string UIDenied            = "ui_denied";
    public const string UIPause             = "ui_pause";
}
