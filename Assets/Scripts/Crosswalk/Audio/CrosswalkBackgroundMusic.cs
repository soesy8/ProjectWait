using System; // 암호문·PCM 배열과 오류 처리를 사용합니다.
using System.Collections; // 복호화한 음원을 나누어 읽어 초기 화면의 프레임을 유지합니다.
using System.Security.Cryptography; // 배포 음원에 AES-256-CBC와 HMAC-SHA256을 적용합니다.
using System.Text; // 암호화 데이터와 WAV의 형식 식별자를 읽습니다.
using UnityEngine; // 기존 씬에서 배경음 AudioSource와 생성한 AudioClip을 관리합니다.
namespace Y.Crosswalk // 횡단보도 파트의 배경음만 담당합니다.
{ // 전체 프로그램의 음악이나 다른 파트의 음원에는 관여하지 않습니다.
    [DefaultExecutionOrder(600)] // 상황 제어기가 이번 프레임에 바꾼 실패 상태를 읽은 뒤 음악을 멈춥니다.
    public sealed class CrosswalkBackgroundMusic : MonoBehaviour // 암호화된 지정 음원을 메모리에서만 재생합니다.
    { // 배경음 전용 AudioSource는 UI 효과음과 분리하여 기존 효과음을 유지합니다.
        [SerializeField, InspectorName("암호화된 배경음"), Tooltip("원본 WAV 대신 배포에 포함할 AES 암호화 음원 데이터입니다.")] private TextAsset m_EncryptedMusic; // 지정한 곡의 암호문만 씬에 연결합니다.
        [SerializeField, InspectorName("배경음 전용 재생기"), Tooltip("UI 효과음과 발소리에 영향을 주지 않는 이 파트의 2D AudioSource입니다.")] private AudioSource m_Source; // 음악의 일시정지와 이어 재생을 전용 재생기에만 적용합니다.
        [SerializeField, Range(0f, 1f), InspectorName("배경음 크기"), Tooltip("대사와 차량 효과음을 가리지 않도록 조절할 배경음 볼륨입니다.")] private float m_Volume = 0.25f; // 지정한 곡을 효과음보다 작게 반복 재생합니다.
        [SerializeField, InspectorName("첫 번째 상황"), Tooltip("전봇대 사고와 재시도 상태를 읽습니다.")] private FirstSituationController m_First; // 첫 상황의 실제 실패 구간을 참조합니다.
        [SerializeField, InspectorName("두 번째 상황"), Tooltip("성급한 횡단과 재시도 상태를 읽습니다.")] private SecondSituationController m_Second; // 깜빡이는 신호의 실패 구간을 참조합니다.
        [SerializeField, InspectorName("세 번째 상황"), Tooltip("차량 위험 접근과 재시도 상태를 읽습니다.")] private ThirdSituationController m_Third; // 파란불 차량 확인 상황의 실패 구간을 참조합니다.
        private const string k_EncryptionKey = "mVlp8j9exMeWUdgoQBneLZeK2sFuV7plXcJp0W/AMYc="; // 배포 암호문을 실행 중 메모리에서 복호화할 AES 키입니다.
        private const string k_AuthenticationKey = "3lC8n0S0JHAULh+IYTFDdxMUmqDZaAeS6pToUQoQ/m0="; // 복호화 전에 음원 데이터의 변조와 손상을 확인할 별도 인증 키입니다.
        private AudioClip m_RuntimeClip; // 디스크 파일 없이 생성한 이번 파트의 배경음 클립입니다.
        private bool m_Ready, m_PausedForFailure, m_StartedPlaying; // 로딩 완료와 실패 일시정지 및 최초 재생을 구분합니다.
        private void OnEnable() // 씬이나 파트가 활성화되면 지정한 음악만 준비합니다.
        { // 헤드셋과 PC 모두 같은 2D 배경음을 사용합니다.
            m_Ready = false; m_StartedPlaying = false; m_Source.playOnAwake = false; m_Source.loop = true; m_Source.spatialBlend = 0f; m_Source.volume = m_Volume; // 새 재생기의 최종 재생 설정을 한곳에서 적용합니다.
            StartCoroutine(LoadMusic()); // 원본을 파일로 풀지 않고 암호문에서 재생 데이터를 준비합니다.
        } // 실패 중 로딩이 끝나더라도 음악을 먼저 재생하지 않습니다.
        private IEnumerator LoadMusic() // 지정한 24비트 PCM 곡을 기존 화면 뒤에서 메모리 클립으로 준비합니다.
        { // 원본 WAV나 복호화 임시 파일을 생성하지 않습니다.
            byte[] wave = null; float[] batch = null; int dataOffset = 0, sampleCount = 0, channels = 0, sampleRate = 0; bool loaded = false; // 복호화한 원본과 변환 중인 짧은 PCM 배열만 보관합니다.
            try // 손상된 암호문과 지원하지 않는 데이터는 재생 전에 중단합니다.
            { // AudioClip 생성은 Unity의 메인 스레드에서 처리합니다.
                wave = DecryptMusic(m_EncryptedMusic.bytes); ReadWave(wave, out dataOffset, out sampleCount, out channels, out sampleRate); // 인증한 암호문에서 지정 음원의 실제 채널과 샘플 수를 읽습니다.
                m_RuntimeClip = AudioClip.Create("횡단보도 배경음", sampleCount / channels, channels, sampleRate, false); loaded = true; // 파일 경로 없이 메모리에서 반복 재생할 클립을 만듭니다.
            } // 인증에 실패하면 잘못된 데이터로 재생을 진행하지 않습니다.
            catch (Exception error) { Debug.LogWarning("횡단보도 배경음을 준비하지 못했습니다: " + error.Message, this); } // 문제는 콘솔에만 알리고 별도 UI 창은 띄우지 않습니다.
            if (!loaded) { if (wave != null) Array.Clear(wave, 0, wave.Length); yield break; } // 재생할 수 없는 데이터는 메모리에서 지우고 로딩을 마칩니다.
            try // 코루틴이 중단되어도 복호화한 PCM 배열을 정리합니다.
            { // 한 프레임에 전체 곡을 변환하지 않도록 짧은 블록으로 나눕니다.
                for (int decoded = 0; decoded < sampleCount;) // 지정한 곡의 실제 데이터 구간만 읽습니다.
                { // 마지막 블록이 클립 처음을 덮어쓰지 않도록 남은 길이에 맞춰 배열을 만듭니다.
                    int count = Mathf.Min(262144, sampleCount - decoded); if (batch == null || batch.Length != count) { if (batch != null) Array.Clear(batch, 0, batch.Length); batch = new float[count]; } // 변환 버퍼는 현재 블록에 필요한 크기만 사용합니다.
                    for (int i = 0; i < count; ++i) { int offset = dataOffset + (decoded + i) * 3; int value = wave[offset] | wave[offset + 1] << 8 | wave[offset + 2] << 16; if ((value & 0x800000) != 0) value |= unchecked((int)0xFF000000); batch[i] = value / 8388608f; } // 원본 24비트 부호 있는 PCM을 재생용 실수 샘플로 변환합니다.
                    m_RuntimeClip.SetData(batch, decoded / channels); decoded += count; yield return null; // 실제 채널별 위치에 기록하고 다음 화면 프레임을 넘깁니다.
                } // 전체 곡의 메모리 클립이 준비되면 원본 배열을 남기지 않습니다.
            } // 중간에 파트가 비활성화된 경우에도 아래 정리를 수행합니다.
            finally { if (wave != null) Array.Clear(wave, 0, wave.Length); if (batch != null) Array.Clear(batch, 0, batch.Length); } // 복호화한 WAV와 중간 샘플 버퍼를 지웁니다.
            m_Source.clip = m_RuntimeClip; m_Ready = true; m_PausedForFailure = IsFailure(); // 로딩이 끝난 시점의 실제 상황 상태로 최초 재생을 결정합니다.
            if (!m_PausedForFailure) { m_Source.Play(); m_StartedPlaying = true; } // 정상 진행 중에만 음악을 시작합니다.
        } // 실패 중에는 첫 재생도 관찰 복귀까지 기다립니다.
        private static byte[] DecryptMusic(byte[] packed) // 암호화된 음원을 인증한 뒤 메모리 안에서만 복호화합니다.
        { // AES 암호화 키와 인증 키를 서로 분리합니다.
            byte[] key = Convert.FromBase64String(k_EncryptionKey), authenticationKey = Convert.FromBase64String(k_AuthenticationKey); // 현재 배포 음원에 대응하는 키를 준비합니다.
            try // 암호문 형식과 인증이 맞을 때만 복호화합니다.
            { // 데이터 끝의 32바이트는 암호문과 IV를 함께 인증하는 값입니다.
                if (packed.Length < 72 || Encoding.ASCII.GetString(packed, 0, 8) != "PWMUS001") throw new CryptographicException("음원 데이터 형식이 올바르지 않습니다."); // 엉뚱한 파일을 음원 암호문으로 처리하지 않습니다.
                int signatureOffset = packed.Length - 32; using (HMACSHA256 hmac = new HMACSHA256(authenticationKey)) // 전체 암호문과 무작위 IV의 인증값을 계산합니다.
                { // 인증값은 첫 다른 바이트에서 끝내지 않고 전체를 비교합니다.
                    byte[] signature = hmac.ComputeHash(packed, 0, signatureOffset); int difference = 0; for (int i = 0; i < signature.Length; ++i) difference |= signature[i] ^ packed[signatureOffset + i]; Array.Clear(signature, 0, signature.Length); // 고정 길이의 모든 인증 바이트를 비교합니다.
                    if (difference != 0) throw new CryptographicException("음원 데이터 인증에 실패했습니다."); // 변조되거나 손상된 데이터는 복호화하지 않습니다.
                } // 인증이 끝난 뒤에만 AES 복호화를 시작합니다.
                using (Aes aes = Aes.Create()) // Unity의 기본 .NET 암호화 구현을 사용합니다.
                { // 매 암호화 파일의 무작위 초기화 벡터를 함께 사용합니다.
                    aes.KeySize = 256; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7; aes.Key = key; byte[] iv = new byte[16]; Buffer.BlockCopy(packed, 8, iv, 0, iv.Length); aes.IV = iv; // 암호화 파일에 기록한 IV와 별도 AES 키를 연결합니다.
                    using (ICryptoTransform decryptor = aes.CreateDecryptor()) return decryptor.TransformFinalBlock(packed, 24, signatureOffset - 24); // WAV 바이트를 임시 파일 없이 반환합니다.
                } // 암호화 핸들과 내부 상태를 해제합니다.
            } // 호출자는 반환된 WAV 배열도 재생 데이터 준비 뒤 지웁니다.
            finally { Array.Clear(key, 0, key.Length); Array.Clear(authenticationKey, 0, authenticationKey.Length); Array.Clear(packed, 0, packed.Length); } // 이번 로딩에 사용한 키와 암호문 복사본을 정리합니다.
        } // 클라이언트에서 재생하는 소리는 녹음이나 메모리 접근까지 완전히 막을 수는 없습니다.
        private static void ReadWave(byte[] wave, out int dataOffset, out int sampleCount, out int channels, out int sampleRate) // 제공된 WAV의 JUNK·fmt·data 구간을 실제 길이에 맞춰 읽습니다.
        { // 현재 지정한 곡의 24비트 PCM 형식만 처리합니다.
            dataOffset = 0; sampleCount = 0; channels = 0; sampleRate = 0; int format = 0, bits = 0, dataLength = 0; // 헤더에서 찾은 재생 형식을 저장합니다.
            if (wave.Length < 44 || Encoding.ASCII.GetString(wave, 0, 4) != "RIFF" || Encoding.ASCII.GetString(wave, 8, 4) != "WAVE") throw new InvalidOperationException("WAV 헤더가 올바르지 않습니다."); // PCM 데이터가 아닌 배열을 재생하지 않습니다.
            for (int offset = 12; offset + 8 <= wave.Length;) // 지정 곡에 포함된 부가 헤더도 건너뛰며 실제 음원 구간을 찾습니다.
            { // 각 구간의 길이는 파일 범위를 벗어나지 않아야 합니다.
                int size = BitConverter.ToInt32(wave, offset + 4); if (size < 0 || size > wave.Length - offset - 8) throw new InvalidOperationException("WAV 구간 길이가 올바르지 않습니다."); string tag = Encoding.ASCII.GetString(wave, offset, 4); // 구간 이름과 실제 데이터 범위를 읽습니다.
                if (tag == "fmt " && size >= 16) { format = BitConverter.ToUInt16(wave, offset + 8); channels = BitConverter.ToUInt16(wave, offset + 10); sampleRate = BitConverter.ToInt32(wave, offset + 12); bits = BitConverter.ToUInt16(wave, offset + 22); } // 원본의 PCM 형식과 채널 및 샘플레이트를 읽습니다.
                else if (tag == "data") { dataOffset = offset + 8; dataLength = size; } // 부가 헤더를 제외한 실제 음악 샘플 구간을 저장합니다.
                offset += 8 + size + (size & 1); // 홀수 길이 구간의 정렬 바이트까지 건너뜁니다.
            } // 원본 샘플을 추정한 위치가 아닌 data 구간에서만 읽습니다.
            if (format != 1 || bits != 24 || channels != 2 || sampleRate != 48000 || dataOffset == 0 || dataLength == 0 || dataLength % (channels * 3) != 0) throw new InvalidOperationException("지정 배경음의 24비트 PCM 형식이 아닙니다."); // 제공된 곡과 다른 형식으로 데이터를 잘못 해석하지 않습니다.
            sampleCount = dataLength / 3; // 채널을 포함한 전체 실수 샘플 수를 계산합니다.
        } // AudioClip에는 원본의 48kHz 스테레오를 유지합니다.
        private bool IsFailure() // 각 상황이 실제로 소유한 실패와 재시도 상태만 읽습니다.
        { // 정상 결과와 다음 상황을 기다리는 동안에는 음악을 멈추지 않습니다.
            return m_First != null && m_First.isActiveAndEnabled && (m_First.CurrentState == FirstSituationController.SituationState.Accident || m_First.CurrentState == FirstSituationController.SituationState.Retry) || m_Second != null && m_Second.isActiveAndEnabled && (m_Second.CurrentState == SecondSituationController.StageState.UnsafeResult || m_Second.CurrentState == SecondSituationController.StageState.Retry) || m_Third != null && m_Third.isActiveAndEnabled && (m_Third.CurrentState == ThirdSituationController.StageState.Danger || m_Third.CurrentState == ThirdSituationController.StageState.Retry); // 실패 연출 진입부터 재시도에서 관찰로 복귀할 때까지 일시정지합니다.
        } // PC와 VR은 같은 상황 상태를 참조합니다.
        private void Update() // 상황 제어기의 상태 전환 뒤 음악만 일시정지하거나 이어 재생합니다.
        { // UI·효과음·전역 시간은 변경하지 않습니다.
            if (!m_Ready) return; bool failed = IsFailure(); if (failed == m_PausedForFailure) return; m_PausedForFailure = failed; // 실제 실패 상태가 바뀐 프레임에만 재생기를 조작합니다.
            if (failed) m_Source.Pause(); else if (m_StartedPlaying) m_Source.UnPause(); else { m_Source.Play(); m_StartedPlaying = true; } // 실패 때는 재생 위치를 보존하고 관찰 복귀 때 그 위치부터 이어서 재생합니다.
        } // 실패가 반복되어도 곡을 처음부터 다시 틀지 않습니다.
        private void OnDisable() // 파트가 닫히면 이 파트가 만든 음악만 정리합니다.
        { // 통합된 다른 파트의 음악과 효과음은 건드리지 않습니다.
            StopAllCoroutines(); m_Ready = false; m_StartedPlaying = false; m_Source.Stop(); m_Source.clip = null; if (m_RuntimeClip != null) Destroy(m_RuntimeClip); m_RuntimeClip = null; // 복호화 코루틴의 정리를 실행하고 생성한 메모리 클립을 해제합니다.
        } // 배포 원본이나 복호화 임시 파일은 만들지 않습니다.
    } // 이 씬의 배경음 처리만 마칩니다.
} // 횡단보도 파트의 범위를 마칩니다.
