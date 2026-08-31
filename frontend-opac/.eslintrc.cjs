/**
 * Cấu hình kiểm tra mã nguồn giao diện quản trị.
 *
 * Chỉ bật những luật bắt được lỗi thật: biến thừa, phụ thuộc hook thiếu, thành phần xuất sai cách
 * khiến Vite mất khả năng nạp nóng. Không bật luật định dạng, vì định dạng đã có Prettier lo và một
 * cảnh báo về dấu phẩy chỉ làm loãng những cảnh báo đáng đọc.
 */
module.exports = {
  root: true,
  env: { browser: true, es2022: true },
  parser: '@typescript-eslint/parser',
  parserOptions: { ecmaVersion: 'latest', sourceType: 'module' },
  plugins: ['@typescript-eslint', 'react-hooks', 'react-refresh'],
  extends: [
    'eslint:recommended',
    'plugin:@typescript-eslint/recommended',
    'plugin:react-hooks/recommended',
  ],
  ignorePatterns: ['dist', 'node_modules', 'coverage', '*.cjs'],
  rules: {
    'react-refresh/only-export-components': ['warn', { allowConstantExport: true }],

    // Biến thừa là dấu hiệu của một đoạn sửa dở; cho phép tiền tố gạch dưới để cố ý bỏ qua tham số.
    '@typescript-eslint/no-unused-vars': [
      'error',
      { argsIgnorePattern: '^_', varsIgnorePattern: '^_' },
    ],

    // any bị cấm ở mã sản phẩm nhưng chỉ cảnh báo trong test, nơi đôi khi phải giả lập kiểu.
    '@typescript-eslint/no-explicit-any': 'error',
  },
  overrides: [
    {
      files: ['**/*.test.ts', '**/*.test.tsx', 'src/test/**'],
      rules: { '@typescript-eslint/no-explicit-any': 'warn' },
    },
  ],
};
