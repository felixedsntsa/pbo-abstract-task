using System;

public interface IKemampuan
{
    string Nama { get; }
    void Gunakan(Robot pengguna, Robot target = null);
    bool DapatDigunakan();
    void ResetCooldown();
}

public interface IStatusEffect
{
    void TerapkanEfek(Robot target);
    void UpdateEfek();
    bool SudahSelesai();
}

public class StunEffect : IStatusEffect
{
    private int durasiTersisa;
    private Robot target;

    public StunEffect(Robot target, int durasi)
    {
        this.target = target;
        this.durasiTersisa = durasi;
    }

    public void TerapkanEfek(Robot target)
    {
        Console.WriteLine($"{target.Nama} terkena efek stun! Tidak bisa bergerak untuk {durasiTersisa} giliran!");
    }

    public void UpdateEfek()
    {
        durasiTersisa--;
    }

    public bool SudahSelesai() => durasiTersisa <= 0;
}

public class ShieldEffect : IStatusEffect
{
    private int durasiTersisa;
    private int bonusArmor;
    private Robot target;

    public ShieldEffect(Robot target, int durasi, int bonusArmor)
    {
        this.target = target;
        this.durasiTersisa = durasi;
        this.bonusArmor = bonusArmor;
    }

    public void TerapkanEfek(Robot target)
    {
        target.TambahArmor(bonusArmor);
        Console.WriteLine($"{target.Nama} mendapat tambahan {bonusArmor} armor untuk {durasiTersisa} giliran!");
    }

    public void UpdateEfek()
    {
        if (--durasiTersisa == 0)
        {
            target.KurangiArmor(bonusArmor);
            Console.WriteLine($"Efek shield pada {target.Nama} telah habis!");
        }
    }

    public bool SudahSelesai() => durasiTersisa <= 0;
}

public abstract class Robot
{
    public string Nama { get; protected set; }
    public int Energi { get; protected set; }
    public int Armor { get; protected set; }
    public int Serangan { get; protected set; }
    protected List<IKemampuan> Kemampuan;
    protected List<IStatusEffect> statusEffects;
    protected int baseArmor;

    protected Robot(string nama, int energi, int armor, int serangan)
    {
        Nama = nama;
        Energi = energi;
        Armor = armor;
        baseArmor = armor;
        Serangan = serangan;
        Kemampuan = new List<IKemampuan>();
        statusEffects = new List<IStatusEffect>();
    }

    public virtual void Serang(Robot target)
    {
        if (IsStunned())
        {
            Console.WriteLine($"{Nama} sedang terstun dan tidak bisa menyerang!");
            return;
        }
        int damage = Math.Max(0, Serangan - target.Armor);
        target.TerimaSerangan(damage);
        Console.WriteLine($"{Nama} menyerang {target.Nama} dengan damage {damage}!");
    }

    public virtual void TerimaSerangan(int damage)
    {
        Energi -= damage;
        Console.WriteLine($"{Nama} menerima {damage} damage! Energi tersisa: {Energi}");
    }

    public void GunakanKemampuan(int index, Robot target = null)
    {
        if (IsStunned())
        {
            Console.WriteLine($"{Nama} sedang terstun dan tidak bisa menggunakan kemampuan!");
            return;
        }
        if (index >= 0 && index < Kemampuan.Count)
        {
            var kemampuan = Kemampuan[index];
            if (kemampuan.DapatDigunakan())
            {
                kemampuan.Gunakan(this, target);
            }
            else
            {
                Console.WriteLine($"Kemampuan {kemampuan.Nama} sedang cooldown!");
            }
        }
    }

    public void PulihkanEnergi(int jumlah)
    {
        Energi += jumlah;
        Console.WriteLine($"{Nama} pulih {jumlah} energi! Energi sekarang: {Energi}");
    }

    public void TambahArmor(int nilai)
    {
        Armor += nilai;
    }

    public void KurangiArmor(int nilai)
    {
        Armor = Math.Max(baseArmor, Armor - nilai);
    }

    public void TambahStatusEffect(IStatusEffect effect)
    {
        statusEffects.Add(effect);
        effect.TerapkanEfek(this);
    }

    public void UpdateStatusEffects()
    {
        foreach (var effect in statusEffects.ToList())
        {
            effect.UpdateEfek();
            if (effect.SudahSelesai())
            {
                statusEffects.Remove(effect);
            }
        }
    }

    public bool IsStunned()
    {
        return statusEffects.Any(e => e is StunEffect);
    }

    public virtual void CetakInformasi()
    {
        Console.WriteLine($"\nInformasi Robot {Nama}:");
        Console.WriteLine($"Energi: {Energi}");
        Console.WriteLine($"Armor: {Armor}");
        Console.WriteLine($"Serangan: {Serangan}");

        if (statusEffects.Any())
        {
            Console.WriteLine("Status Effects aktif:");
            foreach (var effect in statusEffects)
            {
                Console.WriteLine($"- {effect.GetType().Name}");
            }
        }
    }

    public List<IKemampuan> GetKemampuan() => Kemampuan;
}

public class Perbaikan : IKemampuan
{
    private int cooldown = 0;
    private const int COOLDOWN_MAX = 3;
    public string Nama => "Perbaikan";

    public void Gunakan(Robot pengguna, Robot target = null)
    {
        int jumlahPerbaikan = 40;
        pengguna.PulihkanEnergi(jumlahPerbaikan);
        cooldown = COOLDOWN_MAX;
    }

    public bool DapatDigunakan() => cooldown == 0;

    public void ResetCooldown()
    {
        if (cooldown > 0) cooldown--;
    }
}

public class SeranganListrik : IKemampuan
{
    private int cooldown = 0;
    private const int COOLDOWN_MAX = 4;
    public string Nama => "Serangan Listrik";

    public void Gunakan(Robot pengguna, Robot target)
    {
        if (target != null)
        {
            int damage = 20;
            target.TerimaSerangan(damage);
            target.TambahStatusEffect(new StunEffect(target, 2));
            cooldown = COOLDOWN_MAX;
        }
    }

    public bool DapatDigunakan() => cooldown == 0;

    public void ResetCooldown()
    {
        if (cooldown > 0) cooldown--;
    }
}

public class SeranganPlasma : IKemampuan
{
    private int cooldown = 0;
    private const int COOLDOWN_MAX = 5;
    public string Nama => "Serangan Plasma";

    public void Gunakan(Robot pengguna, Robot target)
    {
        if (target != null)
        {
            int armorTarget = target.Armor;
            int damage = Math.Max(0, pengguna.Serangan + (armorTarget / 2));
            target.TerimaSerangan(damage);
            Console.WriteLine($"{pengguna.Nama} menembakkan plasma ke {target.Nama}, menembus armor!");
            cooldown = COOLDOWN_MAX;
        }
    }

    public bool DapatDigunakan() => cooldown == 0;

    public void ResetCooldown()
    {
        if (cooldown > 0) cooldown--;
    }
}

public class PertahananSuper : IKemampuan
{
    private int cooldown = 0;
    private const int COOLDOWN_MAX = 4;
    public string Nama => "Pertahanan Super";

    public void Gunakan(Robot pengguna, Robot target = null)
    {
        int bonusArmor = 15;
        int durasi = 3;
        pengguna.TambahStatusEffect(new ShieldEffect(pengguna, durasi, bonusArmor));
        cooldown = COOLDOWN_MAX;
    }

    public bool DapatDigunakan() => cooldown == 0;

    public void ResetCooldown()
    {
        if (cooldown > 0) cooldown--;
    }
}

public class RobotTempur : Robot
{
    public RobotTempur(string nama, string tipe) : base(nama, 100, 10, 20)
    {
        switch (tipe.ToLower())
        {
            case "penyerang":
                Kemampuan.Add(new SeranganListrik());
                Kemampuan.Add(new SeranganPlasma());
                Serangan += 5;
                break;
            case "pertahanan":
                Kemampuan.Add(new Perbaikan());
                Kemampuan.Add(new PertahananSuper());
                Armor += 5;
                break;
            case "seimbang":
                Kemampuan.Add(new Perbaikan());
                Kemampuan.Add(new SeranganListrik());
                break;
        }
    }
}

public class BosRobot : Robot
{
    public BosRobot(string nama) : base(nama, 200, 20, 30)
    {
        Kemampuan.Add(new Perbaikan());
        Kemampuan.Add(new SeranganListrik());
        Kemampuan.Add(new SeranganPlasma());
        Kemampuan.Add(new PertahananSuper());
    }

    public override void TerimaSerangan(int damage)
    {
        int damageSetelahKetahanan = (int)(damage * 0.8);
        base.TerimaSerangan(damageSetelahKetahanan);
    }

    public void Mati()
    {
        if (Energi <= 0)
        {
            Console.WriteLine($"Bos Robot {Nama} telah dikalahkan!");
        }
    }
}

public class SimulatorPertarungan
{
    private List<Robot> robotPemain;
    private BosRobot bos;
    private int currentRound = 1;

    public SimulatorPertarungan()
    {
        robotPemain = new List<Robot>();
        bos = new BosRobot("Mega Boss");
    }

    public void TambahRobotBaru()
    {
        Console.Write("Masukkan nama robot: ");
        string nama = Console.ReadLine();

        Console.WriteLine("\nPilih tipe robot:");
        Console.WriteLine("1. Penyerang (Spesialis damage dengan Serangan Listrik dan Plasma)");
        Console.WriteLine("2. Pertahanan (Spesialis bertahan dengan Perbaikan dan Shield)");
        Console.WriteLine("3. Seimbang (Kombinasi serangan dan pertahanan)");

        int pilihan;
        do
        {
            Console.Write("Pilihan (1-3): ");
        } while (!int.TryParse(Console.ReadLine(), out pilihan) || pilihan < 1 || pilihan > 3);

        string tipe = pilihan switch
        {
            1 => "penyerang",
            2 => "pertahanan",
            3 => "seimbang",
            _ => "seimbang"
        };

        robotPemain.Add(new RobotTempur(nama, tipe));
        Console.WriteLine($"Robot {nama} tipe {tipe} telah ditambahkan ke tim!");
        Thread.Sleep(1500);
    }

    private void TampilkanStatus()
    {
        Console.Clear();
        Console.WriteLine($"\n=== RONDE {currentRound} ===");
        Console.WriteLine("\n=== STATUS BOS ===");
        bos.CetakInformasi();
        Console.WriteLine("\n=== STATUS ROBOT PEMAIN ===");
        foreach (var robot in robotPemain)
        {
            robot.CetakInformasi();
        }
    }

    private void TungguInput()
    {
        Console.WriteLine("\nTekan ENTER untuk melanjutkan...");
        Console.ReadLine();
        Console.Clear();
    }

    private List<string> GetDaftarKemampuan(Robot robot)
    {
        return robot.GetKemampuan().Select(k => k.Nama).ToList();
    }

    private int PilihMenu(string judul, List<string> opsi)
    {
        while (true)
        {
            Console.WriteLine($"\n{judul}");
            for (int i = 0; i < opsi.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {opsi[i]}");
            }
            Console.Write($"\nPilihan Anda (1-{opsi.Count}): ");

            if (int.TryParse(Console.ReadLine(), out int pilihan) &&
                pilihan >= 1 && pilihan <= opsi.Count)
            {
                return pilihan - 1;
            }
            Console.WriteLine("Pilihan tidak valid! Silakan coba lagi.");
        }
    }

    public void MulaiPertarungan()
    {
        Console.Clear();
        Console.WriteLine("\n=== PERTARUNGAN ROBOT DIMULAI! ===");
        TungguInput();

        while (true)
        {
            TampilkanStatus();

            if (!robotPemain.Any(r => r.Energi > 0))
            {
                Console.WriteLine("\nBOS MENANG! Semua robot pemain telah dikalahkan!");
                break;
            }
            if (bos.Energi <= 0)
            {
                Console.WriteLine("\nPEMAIN MENANG! Bos telah dikalahkan!");
                bos.Mati();
                break;
            }

            foreach (var robot in robotPemain.ToList())
            {
                if (robot.Energi <= 0) continue;

                TampilkanStatus();
                Console.WriteLine($"\n=== GILIRAN {robot.Nama} ===");

                if (robot.IsStunned())
                {
                    Console.WriteLine($"{robot.Nama} sedang terstun dan melewati gilirannya!");
                    TungguInput();
                    continue;
                }

                var kemampuan = GetDaftarKemampuan(robot);
                List<string> opsiAksi = new List<string> { "Serang" };
                opsiAksi.AddRange(kemampuan);
                opsiAksi.Add("Lewati Giliran");

                int aksi = PilihMenu("Pilih aksi:", opsiAksi);

                if (aksi == 0)
                { 
                    robot.Serang(bos);
                }
                else if (aksi > 0)
                { 
                    int kemampuanIndex = aksi - 1;
                    robot.GunakanKemampuan(kemampuanIndex, bos);
                }
                else
                { 
                    Console.WriteLine($"{robot.Nama} melewati gilirannya.");
                }

                foreach (var k in robot.GetKemampuan())
                {
                    k.ResetCooldown();
                }

                robot.UpdateStatusEffects();
                TungguInput();
            }

            if (bos.Energi > 0)
            {
                TampilkanStatus();
                Console.WriteLine($"\n=== GILIRAN BOS ===");

                var robotTarget = robotPemain.Where(r => r.Energi > 0).OrderBy(r => Guid.NewGuid()).FirstOrDefault();
                if (robotTarget != null)
                {
                    bos.Serang(robotTarget);
                }

                bos.UpdateStatusEffects();
                TungguInput();
            }

            bos.UpdateStatusEffects();
        }
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        SimulatorPertarungan simulator = new SimulatorPertarungan();

        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== SIMULATOR PERTARUNGAN ROBOT ===");
            Console.WriteLine("1. Tambah Robot Pemain");
            Console.WriteLine("2. Mulai Pertarungan");
            Console.WriteLine("3. Keluar");
            Console.Write("Pilihan Anda: ");

            if (int.TryParse(Console.ReadLine(), out int pilihan))
            {
                switch (pilihan)
                {
                    case 1:
                        simulator.TambahRobotBaru();
                        break;
                    case 2:
                        simulator.MulaiPertarungan();
                        break;
                    case 3:
                        Console.WriteLine("Keluar dari simulator.");
                        return;
                    default:
                        Console.WriteLine("Pilihan tidak valid! Silakan coba lagi.");
                        break;
                }
            }
            else
            {
                Console.WriteLine("Pilihan tidak valid! Silakan coba lagi.");
            }
        }
    }
}