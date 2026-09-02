import { Link, useNavigate } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { App, Button, Skeleton, Tag, Tooltip } from 'antd';
import { FileTextOutlined, ShoppingCartOutlined } from '@ant-design/icons';
import { readerApi } from '@/api/opac';
import { useSiteSettings } from '@/hooks/useSite';
import { useAuthStore } from '@/stores/authStore';
import { useCartStore } from '@/stores/cartStore';
import type { SearchResult } from '@/types/api';

/**
 * Ảnh bìa của tài liệu.
 *
 * Đo trên kho thật: 444 trên 7.675 biểu ghi có ISBN (5,8%). Không có ISBN thì không nguồn nào tra ra
 * ảnh bìa, mà luận văn, đề tài nghiên cứu và bài giảng điện tử — hơn hai phần ba kho — thì không bao
 * giờ có ảnh trên mạng. Nghĩa là với phần lớn biểu ghi, bìa dựng sẵn **là** ảnh bìa chính thức.
 *
 * Bìa ấy do máy chủ dựng chứ không phải trình duyệt: một trang kết quả có hai chục ô bìa, dựng lại
 * bằng JavaScript mỗi lần tải trang là hai chục lần tính toán thừa mà lại không đặt được bộ nhớ đệm.
 * Máy chủ trả SVG kèm dấu bản, trình duyệt giữ lại cả tuần.
 */
export function Cover({ item }: { item: SearchResult }) {
  // Một địa chỉ duy nhất cho mọi biểu ghi: máy chủ tự quyết trả ảnh thật hay bìa dựng sẵn. Trước
  // đây trình duyệt tự chọn theo cột coverImageUrl, và khi cột ấy trỏ sai chỗ thì cả trang đầy ô
  // ảnh hỏng mà không ai biết.
  return (
    <img
      className="lc-cover"
      src={`/api/public/covers/${item.id}`}
      alt={`Bìa của ${item.title}`}
      loading="lazy"
    />
  );
}

/**
 * Dòng chữ cho biết tài liệu còn mượn được hay không — thứ bạn đọc nhìn trước tiên.
 *
 * Kho hết bản rảnh vì nhiều lý do khác nhau: người khác đang mượn, bản mới nhập chưa kiểm nhận,
 * bản đang sửa chữa. Ở danh sách kết quả thì chỉ nói đúng điều chắc chắn là "chưa có bản sẵn sàng";
 * lý do của từng bản nằm ở trang chi tiết, nơi có đủ thông tin để nói chính xác.
 */
export function Availability({ item }: { item: SearchResult }) {
  if (item.availableItemCount > 0) {
    return <Tag color="green">Còn {item.availableItemCount} bản sẵn sàng</Tag>;
  }

  if (item.itemCount > 0) {
    return <Tag color="orange">Chưa có bản sẵn sàng</Tag>;
  }

  if (item.digitalDocumentCount > 0) {
    return <Tag color="blue">Chỉ có bản số</Tag>;
  }

  return <Tag>Chưa có bản in trong kho</Tag>;
}

/**
 * Nút đặt giữ chỗ dùng chung cho thẻ kết quả và trang chi tiết.
 *
 * Chưa đăng nhập thì đưa sang trang đăng nhập thay vì báo lỗi 401 khô khan: đặt giữ là việc đầu
 * tiên bạn đọc muốn làm khi thấy sách, đừng bắt họ tự đoán phải đăng nhập ở đâu.
 */
export function HoldButton({
  bibId,
  size = 'small',
  onDone,
}: {
  bibId: string;
  size?: 'small' | 'middle';
  onDone?: () => void;
}) {
  const { message } = App.useApp();
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);
  const { data: settings } = useSiteSettings();

  const hold = useMutation({
    mutationFn: () => readerApi.createHold({ bibId }),
    onSuccess: (result) => {
      message.success(
        result.queuePosition <= 1
          ? 'Đã đặt giữ, bạn đang đứng đầu hàng đợi.'
          : `Đã đặt giữ, bạn đứng thứ ${result.queuePosition} trong hàng đợi.`,
      );
      onDone?.();
    },
    onError: (error: Error) => message.error(error.message),
  });

  if (settings?.allowHold === false) {
    return null;
  }

  return (
    <Button
      className="lc-btn-outline"
      size={size}
      loading={hold.isPending}
      onClick={() => {
        if (!user) {
          message.info('Bạn cần đăng nhập bằng số thẻ thư viện để đặt giữ.');
          navigate('/dang-nhap');
          return;
        }
        hold.mutate();
      }}
    >
      Đặt giữ
    </Button>
  );
}

/** Một thẻ kết quả: bìa, nhan đề, dòng mô tả, thẻ trạng thái, hai nút hành động bên phải. */
export function ResultRow({ item }: { item: SearchResult }) {
  const { message } = App.useApp();
  const add = useCartStore((state) => state.add);
  const inCart = useCartStore((state) => state.items.some((row) => row.id === item.id));

  const meta = [
    item.authorMain,
    [item.publisherName, item.publishYear?.toString()].filter(Boolean).join(', '),
    item.documentTypeName,
  ]
    .filter(Boolean)
    .join(' · ');

  return (
    <article className="lc-paper lc-result">
      <div className="lc-result__cover">
        <Link to={`/tai-lieu/${item.id}`}>
          <Cover item={item} />
        </Link>
      </div>

      <div className="lc-result__body">
        <h3 className="lc-result__title">
          <Link to={`/tai-lieu/${item.id}`}>{item.title}</Link>
        </h3>

        <p className="lc-result__meta">{meta}</p>

        <div className="lc-result__tags">
          {/*
            Chỉ số phân loại tách khỏi dòng thông tin, đặt vào ô chữ đều nét.
            Nó là một *mã* chứ không phải một câu: bạn đọc dò theo nó để tìm giá sách, mà dò từng
            ký tự thì chữ đều nét dễ hơn hẳn — và tách ra thì cũng thôi lẫn vào tên nhà xuất bản.
          */}
          {item.ddc ? (
            <span className="lc-ma" title="Chỉ số phân loại DDC">
              {item.ddc}
            </span>
          ) : null}
          <Availability item={item} />
          {item.languageName ? <Tag>{item.languageName}</Tag> : null}
          {item.digitalDocumentCount > 0 ? (
            <Tag icon={<FileTextOutlined />} color="blue">
              {item.digitalDocumentCount} tệp số
            </Tag>
          ) : null}
        </div>
      </div>

      <div className="lc-result__actions">
        <HoldButton bibId={item.id} />
        <Link to={`/tai-lieu/${item.id}`}>
          <Button size="small" block>
            Chi tiết
          </Button>
        </Link>
        <Tooltip title={inCart ? 'Tài liệu đã ở trong giỏ' : 'Thêm vào giỏ tài liệu'}>
          <Button
            size="small"
            type="text"
            icon={<ShoppingCartOutlined />}
            disabled={inCart}
            onClick={() => {
              add(item);
              message.success('Đã thêm vào giỏ tài liệu.');
            }}
          >
            {inCart ? 'Đã trong giỏ' : 'Vào giỏ'}
          </Button>
        </Tooltip>
      </div>
    </article>
  );
}

/** Danh sách kết quả dùng chung cho tra cứu, duyệt danh mục và trang cá nhân. */
export function ResultList({
  items,
  loading,
  emptyText = 'Không tìm thấy tài liệu nào phù hợp.',
  showTips = true,
}: {
  items: SearchResult[];
  loading?: boolean;
  emptyText?: string;
  showTips?: boolean;
}) {
  if (loading) {
    return (
      <div className="lc-results__list">
        {[0, 1, 2].map((index) => (
          <div key={index} className="lc-paper lc-result">
            <Skeleton active avatar={{ shape: 'square', size: 84 }} paragraph={{ rows: 2 }} />
          </div>
        ))}
      </div>
    );
  }

  if (items.length === 0) {
    return (
      <div className="lc-empty">
        <div style={{ fontWeight: 600, marginBottom: 8, color: 'var(--lc-ink)' }}>{emptyText}</div>
        {showTips ? (
          <div style={{ lineHeight: 1.8 }}>
            Thử bớt từ khóa, kiểm tra lại chính tả (gõ không dấu vẫn tìm được), dùng{' '}
            <Link to="/tra-cuu-nang-cao">tra cứu nâng cao</Link> theo tác giả, chủ đề hoặc năm xuất
            bản — hoặc <Link to="/thu-vien-khac">tìm ở thư viện khác (Z39.50)</Link>.
          </div>
        ) : null}
      </div>
    );
  }

  return (
    <div className="lc-results__list">
      {items.map((item) => (
        <ResultRow key={item.id} item={item} />
      ))}
    </div>
  );
}

/** Kệ sách nhỏ dùng ở trang chủ và mục tài liệu liên quan. */
export function ResultShelf({ items }: { items: SearchResult[] }) {
  if (items.length === 0) {
    return <div className="lc-empty">Chưa có tài liệu để hiển thị.</div>;
  }

  return (
    <div
      style={{
        display: 'grid',
        gap: 16,
        gridTemplateColumns: 'repeat(auto-fill, minmax(150px, 1fr))',
      }}
    >
      {items.map((item) => (
        <div key={item.id} className="lc-paper" style={{ padding: 12 }}>
          <Link to={`/tai-lieu/${item.id}`}>
            <Cover item={item} />
          </Link>
          <div className="lc-result__title" style={{ fontSize: 14, marginTop: 8 }}>
            <Link to={`/tai-lieu/${item.id}`}>{item.title}</Link>
          </div>
          <div className="lc-result__meta">{item.authorMain ?? '—'}</div>
          <div style={{ marginTop: 6 }}>
            <Availability item={item} />
          </div>
        </div>
      ))}
    </div>
  );
}
