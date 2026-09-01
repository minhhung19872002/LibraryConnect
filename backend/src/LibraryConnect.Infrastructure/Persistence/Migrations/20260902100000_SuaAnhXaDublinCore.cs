using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryConnect.Infrastructure.Persistence.Migrations;

/// <summary>
/// Sửa lại những biểu ghi đã thu hoạch về bằng bộ ánh xạ Dublin Core cũ.
///
/// Sửa mã nguồn thôi thì chỉ đúng cho biểu ghi thu về từ nay trở đi; thư viện nào đã chạy bản trước
/// vẫn còn nguyên hậu quả trong kho. Đo trên máy phát triển trước khi chạy: 7.466 biểu ghi OAI, và
///
///   · 0/7.466 có trường 008 — trường bắt buộc của MARC 21, thiếu là phần mềm khác từ chối nhận;
///   · 7.464/7.466 có 264$c là dấu thời gian của OAI thay vì năm xuất bản;
///   · toàn bộ có 035$a dạng "(OAI)oai:localhost:..." — mất khả năng truy vết về kho nguồn;
///   · 300$a mang kiểu tệp "application/pdf" thay vì mô tả vật lý;
///   · 65 biểu ghi có mã ngôn ngữ sai ("en_" từ "en_US", "zh" chưa đổi sang "chi").
///
/// Làm thẳng bằng SQL trên cột jsonb chứ không nạp từng biểu ghi qua tầng ứng dụng: bảy nghìn biểu
/// ghi đi qua Entity Framework từng cái một mất hàng chục phút và giữ lượt nâng cấp mở suốt thời
/// gian ấy.
/// </summary>
[DbContext(typeof(LibraryConnectDbContext))]
[Migration("20260902100000_SuaAnhXaDublinCore")]
public partial class SuaAnhXaDublinCore : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ---- Hàm phụ trợ: đổi mã ngôn ngữ sang ISO 639-2/B ------------------------------------
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION bib.lc_ma_ngon_ngu(nguon text) RETURNS text AS $$
            DECLARE goc text;
            BEGIN
                IF nguon IS NULL OR btrim(nguon) = '' THEN RETURN 'und'; END IF;

                goc := lower(split_part(btrim(nguon), '_', 1));
                goc := split_part(goc, '-', 1);

                RETURN CASE goc
                    WHEN 'vi' THEN 'vie'  WHEN 'en' THEN 'eng'  WHEN 'fr' THEN 'fre'
                    WHEN 'de' THEN 'ger'  WHEN 'ru' THEN 'rus'  WHEN 'zh' THEN 'chi'
                    WHEN 'ja' THEN 'jpn'  WHEN 'ko' THEN 'kor'  WHEN 'es' THEN 'spa'
                    WHEN 'pt' THEN 'por'  WHEN 'it' THEN 'ita'  WHEN 'nl' THEN 'dut'
                    WHEN 'th' THEN 'tha'  WHEN 'km' THEN 'khm'  WHEN 'lo' THEN 'lao'
                    WHEN 'deu' THEN 'ger' WHEN 'fra' THEN 'fre' WHEN 'zho' THEN 'chi'
                    WHEN 'nld' THEN 'dut' WHEN 'ces' THEN 'cze' WHEN 'ron' THEN 'rum'
                    ELSE CASE WHEN goc ~ '^[a-z]{3}$' THEN goc ELSE 'und' END
                END;
            END; $$ LANGUAGE plpgsql IMMUTABLE;
            """);

        // ---- Hàm phụ trợ: sửa toàn bộ dataFields của một biểu ghi -----------------------------
        //
        // Gom vào một hàm vì năm việc dưới đây đều phải duyệt cùng một mảng jsonb; làm năm câu
        // UPDATE riêng thì mỗi câu đọc và ghi lại cả mảng ấy một lần.
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION bib.lc_sua_anh_xa_dc(fields jsonb, ma_nguon text)
            RETURNS jsonb AS $$
            DECLARE
                f            jsonb;
                sf           jsonb;
                ket          jsonb := '[]'::jsonb;
                subs         jsonb;
                gia_tri      text;
                tag          text;
                mime         text := NULL;
                co_856       boolean := false;
                co_336       boolean := false;
            BEGIN
                -- Vòng một: nhặt ra kiểu tệp đang nằm nhầm ở trường 300 và xem đã có 856 chưa.
                FOR f IN SELECT * FROM jsonb_array_elements(fields) LOOP
                    tag := f->>'tag';
                    IF tag = '856' THEN co_856 := true; END IF;
                    IF tag = '336' THEN co_336 := true; END IF;
                    IF tag = '300' THEN
                        gia_tri := (SELECT s->>'value' FROM jsonb_array_elements(f->'subfields') s
                                     WHERE s->>'code' = 'a' LIMIT 1);
                        IF gia_tri ~ '^[a-zA-Z0-9.+-]+/[a-zA-Z0-9.+-]+$' THEN mime := gia_tri; END IF;
                    END IF;
                END LOOP;

                -- Vòng hai: dựng lại mảng trường.
                FOR f IN SELECT * FROM jsonb_array_elements(fields) LOOP
                    tag := f->>'tag';

                    -- 300 chỉ mang kiểu tệp thì bỏ hẳn trường: không biết số trang thì để trống.
                    IF tag = '300' AND mime IS NOT NULL THEN CONTINUE; END IF;

                    IF tag = '041' THEN
                        subs := (SELECT jsonb_agg(CASE WHEN s->>'code' = 'a'
                                     THEN jsonb_set(s, '{value}', to_jsonb(bib.lc_ma_ngon_ngu(s->>'value')))
                                     ELSE s END)
                                   FROM jsonb_array_elements(f->'subfields') s);
                        ket := ket || jsonb_build_array(jsonb_set(f, '{subfields}', subs));
                        CONTINUE;
                    END IF;

                    -- 264$c / 260$c: dấu thời gian của OAI không phải năm xuất bản.
                    IF tag IN ('264', '260') THEN
                        subs := (SELECT jsonb_agg(
                                     CASE WHEN s->>'code' = 'c' THEN
                                         CASE
                                             WHEN s->>'value' ~ 'T[0-9]{2}:' THEN
                                                 jsonb_set(s, '{value}', to_jsonb('[không rõ]'::text))
                                             WHEN substring(s->>'value' from '^[0-9]{4}') IS NOT NULL THEN
                                                 jsonb_set(s, '{value}',
                                                     to_jsonb(substring(s->>'value' from '^[0-9]{4}')))
                                             ELSE s
                                         END
                                     ELSE s END)
                                   FROM jsonb_array_elements(f->'subfields') s);
                        ket := ket || jsonb_build_array(jsonb_set(f, '{subfields}', subs));
                        CONTINUE;
                    END IF;

                    -- 035$a: thay "(OAI)oai:localhost:MÃ" bằng "(tên-máy-kho-nguồn)MÃ".
                    IF tag = '035' THEN
                        subs := (SELECT jsonb_agg(
                                     CASE WHEN s->>'code' = 'a' AND s->>'value' LIKE '(OAI)%' THEN
                                         jsonb_set(s, '{value}', to_jsonb(
                                             '(' || ma_nguon || ')' ||
                                             CASE WHEN substring(s->>'value' from 6) LIKE 'oai:%'
                                                  THEN split_part(substring(s->>'value' from 6), ':', 3)
                                                  ELSE substring(s->>'value' from 6)
                                             END))
                                     ELSE s END)
                                   FROM jsonb_array_elements(f->'subfields') s);
                        ket := ket || jsonb_build_array(jsonb_set(f, '{subfields}', subs));
                        CONTINUE;
                    END IF;

                    -- 040$a: tên hiển thị do cán bộ đặt không phải mã cơ quan.
                    IF tag = '040' THEN
                        subs := (SELECT jsonb_agg(CASE WHEN s->>'code' = 'a'
                                     THEN jsonb_set(s, '{value}', to_jsonb(ma_nguon))
                                     ELSE s END)
                                   FROM jsonb_array_elements(f->'subfields') s);
                        ket := ket || jsonb_build_array(jsonb_set(f, '{subfields}', subs));
                        CONTINUE;
                    END IF;

                    ket := ket || jsonb_build_array(f);
                END LOOP;

                -- Kiểu tệp gỡ khỏi 300 thì gắn vào 856$q, đúng chỗ của nó trong MARC 21.
                IF mime IS NOT NULL THEN
                    IF co_856 THEN
                        ket := (SELECT jsonb_agg(
                                    CASE WHEN e->>'tag' = '856'
                                         THEN jsonb_set(e, '{subfields}',
                                                  (e->'subfields') || jsonb_build_array(
                                                      jsonb_build_object('code', 'q', 'value', mime)))
                                         ELSE e END)
                                  FROM jsonb_array_elements(ket) e);
                    ELSE
                        ket := ket || jsonb_build_array(jsonb_build_object(
                            'tag', '856', 'ind1', '4', 'ind2', '0',
                            'subfields', jsonb_build_array(
                                jsonb_build_object('code', 'q', 'value', mime))));
                    END IF;
                END IF;

                -- Bộ ba RDA: bắt buộc theo RDA, mà bộ ánh xạ cũ không dựng bao giờ.
                IF NOT co_336 THEN
                    ket := ket || jsonb_build_array(
                        jsonb_build_object('tag', '336', 'ind1', ' ', 'ind2', ' ',
                            'subfields', jsonb_build_array(
                                jsonb_build_object('code', 'a', 'value', 'text'),
                                jsonb_build_object('code', 'b', 'value', 'txt'),
                                jsonb_build_object('code', '2', 'value', 'rdacontent'))),
                        jsonb_build_object('tag', '337', 'ind1', ' ', 'ind2', ' ',
                            'subfields', jsonb_build_array(
                                jsonb_build_object('code', 'a', 'value',
                                    CASE WHEN co_856 OR mime IS NOT NULL THEN 'computer' ELSE 'unmediated' END),
                                jsonb_build_object('code', 'b', 'value',
                                    CASE WHEN co_856 OR mime IS NOT NULL THEN 'c' ELSE 'n' END),
                                jsonb_build_object('code', '2', 'value', 'rdamedia'))),
                        jsonb_build_object('tag', '338', 'ind1', ' ', 'ind2', ' ',
                            'subfields', jsonb_build_array(
                                jsonb_build_object('code', 'a', 'value',
                                    CASE WHEN co_856 OR mime IS NOT NULL THEN 'online resource' ELSE 'volume' END),
                                jsonb_build_object('code', 'b', 'value',
                                    CASE WHEN co_856 OR mime IS NOT NULL THEN 'cr' ELSE 'nc' END),
                                jsonb_build_object('code', '2', 'value', 'rdacarrier'))));
                END IF;

                RETURN ket;
            END; $$ LANGUAGE plpgsql IMMUTABLE;
            """);

        // ---- Sửa dataFields của mọi biểu ghi thu hoạch qua OAI-PMH ----------------------------
        //
        // Nối biểu ghi về đúng kho nguồn bằng chính trường 040$a: bản cũ ghi tên kho do cán bộ khai
        // vào đó, nên đó là sợi dây duy nhất còn lại giữa biểu ghi và kho. Định danh OAI thì không
        // dùng được — nó chỉ chứa tên máy do kho nguồn tự khai, mà nhiều kho khai "localhost".
        migrationBuilder.Sql("""
            WITH nguon AS (
                SELECT b.id AS bib_id,
                       COALESCE(
                           NULLIF(regexp_replace(r.base_url, '^[a-zA-Z]+://([^/:]+).*$', '\1'),
                                  r.base_url),
                           r.name) AS ma
                  FROM bib.bib_records b
                  JOIN LATERAL (
                        SELECT s->>'value' AS ten
                          FROM jsonb_array_elements(b.marc_data->'dataFields') f,
                               jsonb_array_elements(f->'subfields') s
                         WHERE f->>'tag' = '040' AND s->>'code' = 'a'
                         LIMIT 1) t ON true
                  JOIN ill.oai_repositories r ON r.name = t.ten
                 WHERE b.source = 'Oai' AND b.deleted_at IS NULL
            )
            UPDATE bib.bib_records b
               SET marc_data = jsonb_set(b.marc_data, '{dataFields}',
                       bib.lc_sua_anh_xa_dc(b.marc_data->'dataFields', nguon.ma))
              FROM nguon
             WHERE nguon.bib_id = b.id;
            """);

        // Kho nguồn đã bị xoá hoặc đổi tên thì vẫn phải sửa, chỉ là không suy được tên máy nữa.
        migrationBuilder.Sql("""
            UPDATE bib.bib_records b
               SET marc_data = jsonb_set(b.marc_data, '{dataFields}',
                       bib.lc_sua_anh_xa_dc(b.marc_data->'dataFields', 'khong-ro-nguon'))
             WHERE b.source = 'Oai'
               AND b.deleted_at IS NULL
               AND EXISTS (SELECT 1 FROM jsonb_array_elements(b.marc_data->'dataFields') f,
                                        jsonb_array_elements(f->'subfields') s
                            WHERE f->>'tag' = '035' AND s->>'value' LIKE '(OAI)%');
            """);

        // ---- Dựng trường 008 cho biểu ghi còn thiếu -------------------------------------------
        //
        // Ghép đúng theo quy tắc của Marc008Builder: 00-05 ngày tạo, 06 's', 07-10 năm xuất bản
        // (lấy từ cột phẳng publish_year, không có thì để '||||'), 15-17 mã nước, 18-34 ký tự '|'
        // trừ vị trí 32 để trống, 35-37 mã ngôn ngữ khớp 041$a, 38 trống, 39 'd'.
        migrationBuilder.Sql("""
            UPDATE bib.bib_records b
               SET marc_data = jsonb_set(b.marc_data, '{controlFields}',
                       COALESCE(b.marc_data->'controlFields', '[]'::jsonb) || jsonb_build_array(
                           jsonb_build_object('tag', '008', 'value',
                               to_char(COALESCE(b.created_at, now()), 'YYMMDD')
                               || 's'
                               || CASE WHEN b.publish_year BETWEEN 1000 AND 2999
                                       THEN lpad(b.publish_year::text, 4, '0') ELSE '||||' END
                               || '    '
                               || 'vm '
                               || '||||||||||||||' || ' ' || '||'
                               || COALESCE((SELECT bib.lc_ma_ngon_ngu(s->>'value')
                                              FROM jsonb_array_elements(b.marc_data->'dataFields') f,
                                                   jsonb_array_elements(f->'subfields') s
                                             WHERE f->>'tag' = '041' AND s->>'code' = 'a' LIMIT 1),
                                           'und')
                               || ' d')))
             WHERE b.deleted_at IS NULL
               AND NOT (COALESCE(b.marc_data->'controlFields', '[]'::jsonb) @> '[{"tag":"008"}]');
            """);

        migrationBuilder.Sql("DROP FUNCTION IF EXISTS bib.lc_sua_anh_xa_dc(jsonb, text);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Không phục hồi: dữ liệu cũ là dữ liệu sai, giữ lại đường quay về chỗ sai không có ích gì.
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS bib.lc_ma_ngon_ngu(text);");
    }
}
