import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Breadcrumb, Button, Card, Col, Empty, List, Row, Space, Tag, Typography } from 'antd';
import { opacApi } from '@/api/opac';
import type { BrowseEntry } from '@/types/api';

const { Paragraph } = Typography;

const ALPHABET = 'ABCDEFGHIKLMNOPQRSTUVXY'.split('');

type BrowseKind = 'chu-de' | 'tac-gia' | 'phan-loai' | 'bo-suu-tap' | 'nganh';

const TITLES: Record<BrowseKind, string> = {
  'chu-de': 'Duyệt theo chủ đề',
  'tac-gia': 'Duyệt theo tác giả',
  'phan-loai': 'Duyệt theo khung phân loại',
  'bo-suu-tap': 'Duyệt theo bộ sưu tập',
  nganh: 'Duyệt theo ngành đào tạo',
};

const HINTS: Record<BrowseKind, string> = {
  'chu-de': 'Bấm vào một chủ đề để xem tài liệu; chủ đề có mũi tên thì còn nhánh nhỏ bên dưới.',
  'tac-gia': 'Chọn chữ cái đầu của tên tác giả để thu hẹp danh sách.',
  'phan-loai': 'Cây phân loại theo khung DDC; chọn một nhánh để xem toàn bộ tài liệu thuộc nhánh đó.',
  'bo-suu-tap': 'Các bộ sưu tập do thư viện tổ chức.',
  nganh: 'Chọn ngành để xem danh sách môn học, rồi xem tài liệu của từng môn.',
};

/** IX.2 — Duyệt theo danh mục: chủ đề, tác giả, phân loại, bộ sưu tập, ngành – môn học. */
export function BrowsePage() {
  const { kind = 'chu-de' } = useParams<{ kind: BrowseKind }>();
  const navigate = useNavigate();
  const [trail, setTrail] = useState<BrowseEntry[]>([]);
  const [letter, setLetter] = useState<string | undefined>();

  const parentId = trail.length > 0 ? trail[trail.length - 1]?.id : undefined;

  const { data, isLoading } = useQuery<BrowseEntry[]>({
    queryKey: ['browse', kind, parentId, letter],
    queryFn: () => {
      switch (kind) {
        case 'tac-gia':
          return opacApi.browseAuthors(letter);
        case 'phan-loai':
          return opacApi.browseClassifications(parentId);
        case 'bo-suu-tap':
          return opacApi.browseCollections();
        case 'nganh':
          return opacApi.browseMajors();
        default:
          return opacApi.browseSubjects(parentId);
      }
    },
  });

  const openEntry = (entry: BrowseEntry) => {
    if (kind === 'nganh') {
      navigate(`/duyet/nganh/${entry.id}`);
      return;
    }

    if (entry.hasChildren) {
      setTrail((current) => [...current, entry]);
      return;
    }

    switch (kind) {
      case 'tac-gia':
        navigate(`/tra-cuu?authorId=${entry.id}`);
        break;
      case 'phan-loai':
        navigate(`/tra-cuu?ddc=${encodeURIComponent(entry.code)}`);
        break;
      case 'bo-suu-tap':
        navigate(`/tra-cuu?collectionId=${entry.id}`);
        break;
      default:
        navigate(`/tra-cuu?subjectId=${entry.id}`);
    }
  };

  const title = TITLES[kind as BrowseKind] ?? TITLES['chu-de'];

  return (
    <div className="lc-container" style={{ padding: '24px 16px 48px' }}>
      <Card title={title}>
        <Paragraph type="secondary">{HINTS[kind as BrowseKind] ?? HINTS['chu-de']}</Paragraph>

        {trail.length > 0 ? (
          <Breadcrumb
            style={{ marginBottom: 12 }}
            items={[
              { title: <a onClick={() => setTrail([])}>Tất cả</a> },
              ...trail.map((entry, index) => ({
                title: (
                  <a onClick={() => setTrail((current) => current.slice(0, index + 1))}>
                    {entry.name}
                  </a>
                ),
              })),
            ]}
          />
        ) : null}

        {kind === 'tac-gia' ? (
          <Space size={[4, 4]} wrap style={{ marginBottom: 16 }}>
            <Button size="small" type={letter ? 'default' : 'primary'} onClick={() => setLetter(undefined)}>
              Tất cả
            </Button>
            {ALPHABET.map((character) => (
              <Button
                key={character}
                size="small"
                type={letter === character ? 'primary' : 'default'}
                onClick={() => setLetter(character)}
              >
                {character}
              </Button>
            ))}
          </Space>
        ) : null}

        <List
          loading={isLoading}
          dataSource={data ?? []}
          locale={{ emptyText: <Empty description="Chưa có mục nào có tài liệu." /> }}
          grid={{ gutter: 12, xs: 1, sm: 2, md: 3, lg: 4 }}
          renderItem={(entry) => (
            <List.Item>
              <Card size="small" hoverable onClick={() => openEntry(entry)}>
                <div style={{ fontWeight: 600 }}>
                  {/* Ký hiệu phân loại tự sinh từ biểu ghi có tên trùng luôn với mã; hiện hai lần
                      trông như lỗi hiển thị. */}
                  {kind === 'phan-loai' && entry.code !== entry.name ? `${entry.code} — ` : ''}
                  {entry.name}
                </div>
                <Space size={4} style={{ marginTop: 6 }}>
                  <Tag color="green">
                    {entry.bibCount} {kind === 'nganh' ? 'môn học' : 'tài liệu'}
                  </Tag>
                  {entry.hasChildren && kind !== 'nganh' ? <Tag>có nhánh con</Tag> : null}
                </Space>
              </Card>
            </List.Item>
          )}
        />
      </Card>
    </div>
  );
}

/** Danh sách môn học của một ngành, và tài liệu của môn được chọn (X.3 nhìn từ phía bạn đọc). */
export function MajorCoursesPage() {
  const { majorId = '' } = useParams();
  const [courseId, setCourseId] = useState<string | undefined>();

  const courses = useQuery<BrowseEntry[]>({
    queryKey: ['browse', 'courses', majorId],
    queryFn: () => opacApi.browseCourses(majorId),
  });

  const documents = useQuery({
    queryKey: ['course-documents', majorId, courseId],
    queryFn: () => opacApi.courseDocuments(majorId, courseId!),
    enabled: Boolean(courseId),
  });

  return (
    <div className="lc-container" style={{ padding: '24px 16px 48px' }}>
      <Row gutter={24}>
        <Col xs={24} md={9}>
          <Card title="Môn học" loading={courses.isLoading}>
            <List
              dataSource={courses.data ?? []}
              locale={{ emptyText: <Empty description="Ngành này chưa khai báo môn học." /> }}
              renderItem={(course) => (
                <List.Item
                  onClick={() => setCourseId(course.id)}
                  style={{
                    cursor: 'pointer',
                    background: courseId === course.id ? '#eef6f2' : undefined,
                    padding: '10px 12px',
                    borderRadius: 6,
                  }}
                >
                  <List.Item.Meta
                    title={`${course.code} — ${course.name}`}
                    description={`${course.bibCount} tài liệu`}
                  />
                </List.Item>
              )}
            />
          </Card>
        </Col>

        <Col xs={24} md={15}>
          <Card title="Tài liệu của môn học" loading={documents.isFetching}>
            {!courseId ? (
              <Empty description="Chọn một môn học ở bên trái." />
            ) : (
              <List
                dataSource={documents.data?.items ?? []}
                locale={{ emptyText: <Empty description="Môn học này chưa được gán tài liệu." /> }}
                renderItem={(row) => (
                  <List.Item>
                    <List.Item.Meta
                      title={<Link to={`/tai-lieu/${row.bib.id}`}>{row.bib.title}</Link>}
                      description={
                        <Space size={[8, 4]} wrap>
                          <Tag color="green">{row.relationLabel}</Tag>
                          <span>{row.bib.authorMain}</span>
                          {row.bib.availableItemCount > 0 ? (
                            <Tag color="green">Còn {row.bib.availableItemCount} bản</Tag>
                          ) : (
                            <Tag color="orange">Hết bản rảnh</Tag>
                          )}
                          {row.note ? <span>{row.note}</span> : null}
                        </Space>
                      }
                    />
                  </List.Item>
                )}
              />
            )}
          </Card>
        </Col>
      </Row>
    </div>
  );
}
