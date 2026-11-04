import type { ReactNode } from 'react';
import AccountBalanceIcon from '@mui/icons-material/AccountBalance';
import BadgeIcon from '@mui/icons-material/Badge';
import StorefrontIcon from '@mui/icons-material/Storefront';
import CategoryIcon from '@mui/icons-material/Category';

/** 準備中（ダミー）メニュー */
export interface SimpleMenuItem {
  label: string;
  path: string;
  icon: ReactNode;
  dummy?: boolean;
}

export const dummyMenus: SimpleMenuItem[] = [
  { label: '口座', path: '/accounts', icon: <AccountBalanceIcon />, dummy: true },
  { label: '発注担当コード', path: '/order-staff', icon: <BadgeIcon />, dummy: true },
  { label: '取引先', path: '/partners', icon: <StorefrontIcon />, dummy: true },
  { label: '受注型式', path: '/order-types', icon: <CategoryIcon />, dummy: true },
];
