/** Kiểu dữ liệu của phân hệ Quản trị nội dung (Phân hệ VIII). */

export interface CmsPageRow {
  id: string;
  slug: string;
  title: string;
  metaDescription?: string;
  isPublished: boolean;
  publishedAt?: string;
  viewCount: number;
  sortOrder: number;
  parentId?: string;
  updatedAt?: string;
}

export interface CmsPage extends Omit<CmsPageRow, 'updatedAt'> {
  content?: string;
}

export interface CmsNewsRow {
  id: string;
  title: string;
  slug: string;
  summary?: string;
  thumbnailUrl?: string;
  categoryId?: string;
  categoryName?: string;
  tags?: string;
  author?: string;
  isFeatured: boolean;
  isPublished: boolean;
  publishedAt?: string;
  viewCount: number;
  updatedAt?: string;
}

export interface CmsNews extends Omit<CmsNewsRow, 'updatedAt'> {
  content?: string;
}

export interface CmsBanner {
  id: string;
  title: string;
  imageUrl: string;
  link?: string;
  position: string;
  sortOrder: number;
  startDate?: string;
  endDate?: string;
  isActive: boolean;
}

export interface CmsMenu {
  id: string;
  name: string;
  url?: string;
  parentId?: string;
  sortOrder: number;
  target?: string;
  icon?: string;
  isActive: boolean;
  children: CmsMenu[];
}

export interface CmsLink {
  id: string;
  name: string;
  url: string;
  logoUrl?: string;
  description?: string;
  groupName?: string;
  sortOrder: number;
  isActive: boolean;
}

export interface CmsGalleryImage {
  id?: string;
  imageUrl: string;
  caption?: string;
  sortOrder: number;
}

export interface CmsGallery {
  id: string;
  title: string;
  description?: string;
  coverUrl?: string;
  eventDate?: string;
  isPublished: boolean;
  images: CmsGalleryImage[];
}

export interface CmsSettingItem {
  key: string;
  value?: string;
  name: string;
  description?: string;
  dataType: string;
  groupCode: string;
  groupName: string;
  /** PARAMETER hoặc CMS — cho biết giá trị được ghi về kho nào. */
  store: string;
  sortOrder: number;
}

export interface CmsSettingGroup {
  code: string;
  name: string;
  items: CmsSettingItem[];
}

export interface CmsReviewRow {
  id: string;
  bibId: string;
  bibTitle: string;
  readerId: string;
  readerName: string;
  readerCardNumber?: string;
  rating: number;
  comment?: string;
  isApproved: boolean;
  createdAt: string;
}

export interface CmsMedia {
  objectName: string;
  url: string;
  contentType: string;
  sizeBytes: number;
}

export interface CmsNewsStatistics {
  totalCount: number;
  publishedCount: number;
  draftCount: number;
  totalViews: number;
  byCategory: { categoryName: string; newsCount: number; viewCount: number }[];
  topViewed: { id: string; title: string; slug: string; viewCount: number; publishedAt?: string }[];
}
